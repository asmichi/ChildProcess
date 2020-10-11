// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Unix
{
    internal class UnixConsolePal : IConsolePal
    {
        private static readonly SafeFileHandle StdInputHandle = new SafeFileHandle(new IntPtr(0), ownsHandle: false);
        private static readonly SafeFileHandle StdOutputHandle = new SafeFileHandle(new IntPtr(1), ownsHandle: false);
        private static readonly SafeFileHandle StdErrorHandle = new SafeFileHandle(new IntPtr(2), ownsHandle: false);

        public SafeFileHandle GetStdInputHandleForChild() => StdInputHandle;
        public SafeFileHandle GetStdOutputHandleForChild() => StdOutputHandle;
        public SafeFileHandle GetStdErrorHandleForChild() => StdErrorHandle;

        public bool HasConsoleWindow() => true;
    }
}
