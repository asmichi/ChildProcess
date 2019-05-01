// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Linux
{
    internal static class ConsolePalLinux
    {
        public static SafeFileHandle GetStdInputHandleForChild() => throw new NotImplementedException();
        public static SafeFileHandle GetStdOutputHandleForChild() => throw new NotImplementedException();
        public static SafeFileHandle GetStdErrorHandleForChild() => throw new NotImplementedException();

        public static bool HasConsoleWindow() => throw new NotImplementedException();
    }
}
