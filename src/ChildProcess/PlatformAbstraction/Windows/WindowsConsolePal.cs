// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.ProcessManagement;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal sealed class WindowsConsolePal : IConsolePal
    {
        public SafeFileHandle GetStdInputHandleForChild(bool createNewConsole) =>
            GetStdHandleForChild(Kernel32.STD_INPUT_HANDLE, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Read);
        public SafeFileHandle GetStdOutputHandleForChild(bool createNewConsole) =>
            GetStdHandleForChild(Kernel32.STD_OUTPUT_HANDLE, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Write);
        public SafeFileHandle GetStdErrorHandleForChild(bool createNewConsole) =>
            GetStdHandleForChild(Kernel32.STD_ERROR_HANDLE, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Write);

        /// <summary>
        /// Returns the std* handle of the current process that can be inherited by a child process.
        /// This handle can only be used if the child process share the same console with the current process
        /// (that is, <see cref="ChildProcessFlags.CreateNewConsole"/> is not set and the current process
        /// is attached to a console).
        /// </summary>
        /// <returns>The std* handle of the current process. <see langword="null"/> if the current process does not have any. </returns>
        private static SafeFileHandle? GetStdHandleForChild(int kind, bool createNewConsole)
        {
            // GetStdHandle may return INVALID_HANDLE_VALUE on success because one can perform SetStdHandle(..., INVALID_HANDLE_VALUE).
            var handleValue = Kernel32.GetStdHandle(kind);
            if (handleValue == IntPtr.Zero || handleValue == Kernel32.InvalidHandleValue)
            {
                return null;
            }

            var handle = new SafeFileHandle(handleValue, false);

            // Console handles can be inherited only by a child process within the same console.
            // If the child process will be attached to a new pseudo console,
            // ignore console handles and redirect from/to NUL instead.
            if (createNewConsole && IsConsoleHandle(handle))
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

        public bool HasConsoleWindow() => Kernel32.GetConsoleWindow() != IntPtr.Zero;
    }
}
