// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal sealed class WindowsConsolePal : IConsolePal
    {
        public SafeFileHandle? GetStdInputHandleForChild() => GetStdHandleForChild(Kernel32.STD_INPUT_HANDLE);
        public SafeFileHandle? GetStdOutputHandleForChild() => GetStdHandleForChild(Kernel32.STD_OUTPUT_HANDLE);
        public SafeFileHandle? GetStdErrorHandleForChild() => GetStdHandleForChild(Kernel32.STD_ERROR_HANDLE);

        // Returns the std* handle of the current process that can be inherited by a child process.
        private SafeFileHandle? GetStdHandleForChild(int kind)
        {
            var handle = new SafeFileHandle(Kernel32.GetStdHandle(kind), false);

            // If we do not have a console window, we attach a new console to a child process.
            // In this case console pseudo handles cannot be inherited by a child process.
            // Ignore console pseudo handles and redirect from/to NUL instead, so that
            // our child processes should not stuck reading from an invisible console.
            if (handle.IsInvalid
                || (IsConsoleHandle(handle) && !HasConsoleWindow()))
            {
                handle.Dispose();
                return null;
            }

            return handle;
        }

        private static bool IsConsoleHandle(SafeFileHandle handle)
        {
            // Make sure the handle is a character device.
            // Maybe calling GetConsoleMode is enough, though.
            int fileType = Kernel32.GetFileType(handle);
            if (fileType == Kernel32.FILE_TYPE_UNKNOWN || (fileType & Kernel32.FILE_TYPE_CHAR) == 0)
            {
                return false;
            }

            // If GetConsoleMode succeeds, handle is a console handle.
            return Kernel32.GetConsoleMode(handle, out var _);
        }

        public bool HasConsoleWindow()
        {
            return Kernel32.GetConsoleWindow() != IntPtr.Zero;
        }
    }
}
