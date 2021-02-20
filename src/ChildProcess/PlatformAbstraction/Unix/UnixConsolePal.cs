// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.Utilities.Interop.Linux;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Unix
{
    internal class UnixConsolePal : IConsolePal
    {
        private const int StdInFileNo = 0;
        private const int StdOutFileNo = 1;
        private const int StdErrFileNo = 2;

        public SafeFileHandle GetStdInputHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdInFileNo, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Read);
        public SafeFileHandle GetStdOutputHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdOutFileNo, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Write);
        public SafeFileHandle GetStdErrorHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdErrFileNo, createNewConsole) ?? FilePal.OpenNullDevice(System.IO.FileAccess.Write);

        private static SafeFileHandle? DuplicateStdFileForChild(int stdFd, bool createNewConsole)
        {
            if (!LibChildProcess.DuplicateStdFileForChild(stdFd, createNewConsole, out var newFd))
            {
                // Probably EBADF.
                return null;
            }

            return newFd.IsInvalid ? null : newFd;
        }

        public bool HasConsoleWindow() => true;
    }
}
