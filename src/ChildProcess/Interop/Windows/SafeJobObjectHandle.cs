// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Interop.Windows
{
    internal sealed class SafeJobObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeJobObjectHandle()
            : this(IntPtr.Zero, true)
        {
        }

        public SafeJobObjectHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        public SafeJobObjectHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            Kernel32.CloseHandle(handle);
            return true;
        }
    }
}
