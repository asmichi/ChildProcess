// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Windows
{
    internal sealed class ProcThreadAttributeList : IDisposable
    {
        private readonly SafeUnmanagedProcThreadAttributeList _unmanaged;
        private bool _isDisposed;

        public ProcThreadAttributeList(int attributeCount)
        {
            _unmanaged = SafeUnmanagedProcThreadAttributeList.Create(attributeCount);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _unmanaged.Dispose();
                _isDisposed = true;
            }
        }

        public unsafe void UpdateHandleList(IntPtr* handles, int count)
        {
            if (!Kernel32.UpdateProcThreadAttribute(
                _unmanaged,
                0,
                Kernel32.PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                handles,
                new IntPtr(sizeof(IntPtr) * count),
                IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new Win32Exception();
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public IntPtr DangerousGetHandle() => _unmanaged.DangerousGetHandle();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void DangerousAddRef(ref bool success) => _unmanaged.DangerousAddRef(ref success);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void DangerousRelease() => _unmanaged.DangerousRelease();
    }

    internal sealed class SafeUnmanagedProcThreadAttributeList : SafeHandleZeroOrMinusOneIsInvalid
    {
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private SafeUnmanagedProcThreadAttributeList()
            : this(IntPtr.Zero)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public SafeUnmanagedProcThreadAttributeList(IntPtr memory)
            : base(true)
        {
            SetHandle(memory);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseHandle()
        {
            // Must be DeleteProcThreadAttributeList'ed before freed.
            Kernel32.DeleteProcThreadAttributeList(handle);
            Marshal.FreeHGlobal(handle);
            return true;
        }

        public static SafeUnmanagedProcThreadAttributeList Create(int attributeCount)
        {
            var result = default(SafeUnmanagedProcThreadAttributeList);
            var size = IntPtr.Zero;
            int win32Error = 0;

            Kernel32.InitializeProcThreadAttributeList(IntPtr.Zero, attributeCount, 0, ref size);

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                var buffer = Marshal.AllocHGlobal(size.ToInt32());

                if (!Kernel32.InitializeProcThreadAttributeList(buffer, attributeCount, 0, ref size))
                {
                    win32Error = Marshal.GetLastWin32Error();
                    Marshal.FreeHGlobal(buffer);
                }

                result = new SafeUnmanagedProcThreadAttributeList(buffer);
            }

            if (win32Error != 0)
            {
                throw new Win32Exception(win32Error);
            }

            return result;
        }
    }
}
