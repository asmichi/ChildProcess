// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Interop.Windows
{
    internal sealed class SafePseudoConsoleHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePseudoConsoleHandle()
            : this(IntPtr.Zero, true)
        {
        }

        public SafePseudoConsoleHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        public SafePseudoConsoleHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            Kernel32.ClosePseudoConsole(handle);
            return true;
        }

        // inputReader and outputWriter can be closed after this method returns.
        // outputWriter can be a handle to the null device.
        public static SafePseudoConsoleHandle Create(SafeHandle inputReader, SafeHandle outputWriter)
        {
            // The console size does not matter since we redirect... maybe?
            var consoleSize = new Kernel32.COORD { X = 80, Y = 25 };
            int hr = Kernel32.CreatePseudoConsole(consoleSize, inputReader, outputWriter, 0, out var hPC);
            Marshal.ThrowExceptionForHR(hr);
            return new SafePseudoConsoleHandle(hPC);
        }
    }
}
