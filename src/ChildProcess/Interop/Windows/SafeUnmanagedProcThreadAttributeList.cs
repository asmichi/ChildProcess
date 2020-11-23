// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Windows
{
    internal sealed class SafeUnmanagedProcThreadAttributeList : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeUnmanagedProcThreadAttributeList()
            : this(IntPtr.Zero)
        {
        }

        public SafeUnmanagedProcThreadAttributeList(IntPtr memory)
            : base(true)
        {
            SetHandle(memory);
        }

        protected override bool ReleaseHandle()
        {
            // Must be DeleteProcThreadAttributeList'ed before freed.
            Kernel32.DeleteProcThreadAttributeList(handle);
            Marshal.FreeHGlobal(handle);
            return true;
        }

        public static SafeUnmanagedProcThreadAttributeList Create(int attributeCount)
        {
            var size = IntPtr.Zero;

            Kernel32.InitializeProcThreadAttributeList(IntPtr.Zero, attributeCount, 0, ref size);

            var buffer = Marshal.AllocHGlobal(size.ToInt32());

            if (!Kernel32.InitializeProcThreadAttributeList(buffer, attributeCount, 0, ref size))
            {
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception();
            }

            return new SafeUnmanagedProcThreadAttributeList(buffer);
        }
    }
}
