// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.Interop.Linux;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.PlatformAbstraction.Unix
{
    internal class UnixConsolePal : IConsolePal
    {
        private const int StdInFileNo = 0;
        private const int StdOutFileNo = 1;
        private const int StdErrFileNo = 2;

        public SafeFileHandle? CreateStdInputHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdInFileNo, createNewConsole);
        public SafeFileHandle? CreateStdOutputHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdOutFileNo, createNewConsole);
        public SafeFileHandle? CreateStdErrorHandleForChild(bool createNewConsole) =>
            DuplicateStdFileForChild(StdErrFileNo, createNewConsole);

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
