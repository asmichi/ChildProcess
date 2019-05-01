// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Windows
{
    internal sealed class SafeAnyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeAnyHandle()
            : this(IntPtr.Zero, true)
        {
        }

        public SafeAnyHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        public SafeAnyHandle(IntPtr existingHandle, bool ownsHandle)
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
