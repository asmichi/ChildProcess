// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
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

        public unsafe void UpdatePseudoConsole(IntPtr hPC)
        {
            if (!Kernel32.UpdateProcThreadAttribute(
                _unmanaged,
                0,
                Kernel32.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                hPC.ToPointer(),
                new IntPtr(sizeof(IntPtr)),
                IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new Win32Exception();
            }
        }

        public IntPtr DangerousGetHandle() => _unmanaged.DangerousGetHandle();
        public void DangerousAddRef(ref bool success) => _unmanaged.DangerousAddRef(ref success);
        public void DangerousRelease() => _unmanaged.DangerousRelease();
    }
}
