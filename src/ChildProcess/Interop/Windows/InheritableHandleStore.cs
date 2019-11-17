// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.Interop.Windows
{
    /// <summary>
    /// Stores inheritable handles. When an uninheritable handle would be added, creates and stores a temporary inheritable duplicate of it.
    /// Typically those handles will be passed to UpdateProcThreadAttribute.
    /// </summary>
    internal sealed class InheritableHandleStore : IDisposable
    {
        private readonly Element[] _elements;
        private bool _isDisposed;
        private int _count;

        public InheritableHandleStore(int capacity)
        {
            _elements = new Element[capacity];
        }

        public int Count => _count;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                for (int i = 0; i < _count; i++)
                {
                    // If InheritableHandle is the one we created, close it.
                    if (_elements[i].OriginalHandle != _elements[i].InheritableHandle)
                    {
                        _elements[i].InheritableHandle.Dispose();
                    }
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Returns inheritable handles stored.
        /// Make sure to keep a reference to this <see cref="InheritableHandleStore"/> while the handles are being used.
        /// The handles will be invalid when InheritableHandleStore is GCed.
        /// </summary>
        /// <param name="buffer">Buffer to receive inheritable handles.</param>
        public void DangerousGetHandles(Span<IntPtr> buffer)
        {
            int count = _count;

            for (int i = 0; i < count; i++)
            {
                buffer[i] = _elements[i].InheritableHandle.DangerousGetHandle();
            }
        }

        /// <summary>
        /// Add a handle to this.
        /// If <paramref name="handle"/> is not inheritable, returns an inheritable duplicate. Otherwise returns <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">A handle.</param>
        /// <returns>An inheritable handle that points to the same object as <paramref name="handle"/>.</returns>
        public SafeHandle Add(SafeHandle handle)
        {
            // Avoid adding an existing handle.
            // UpdateProcThreadAttribute(..., PROC_THREAD_ATTRIBUTE_HANDLE_LIST, ...) will fail
            // when the list of handles contains dupliate entries.
            for (int i = 0; i < _count; i++)
            {
                if (_elements[i].OriginalHandle.DangerousGetHandle() == handle.DangerousGetHandle())
                {
                    return _elements[i].InheritableHandle;
                }
            }

            var inheritableHandle = DuplicateIfUninheritable(handle);
            _elements[_count++] = new Element(handle, inheritableHandle);
            return inheritableHandle;
        }

        private static SafeHandle DuplicateIfUninheritable(SafeHandle handle)
        {
            if (!Kernel32.GetHandleInformation(handle, out int flags))
            {
                throw new Win32Exception();
            }

            if ((flags & Kernel32.HANDLE_FLAG_INHERIT) != 0)
            {
                return handle;
            }
            else
            {
                if (!Kernel32.DuplicateHandle(
                    Kernel32.GetCurrentProcess(),
                    handle,
                    Kernel32.GetCurrentProcess(),
                    out SafeAnyHandle inheritableHandle,
                    0,
                    true,
                    Kernel32.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }

                return inheritableHandle;
            }
        }

        private readonly struct Element
        {
            public readonly SafeHandle OriginalHandle;
            public readonly SafeHandle InheritableHandle;

            public Element(SafeHandle originalHandle, SafeHandle inheritableHandle)
            {
                OriginalHandle = originalHandle;
                InheritableHandle = inheritableHandle;
            }
        }
    }
}
