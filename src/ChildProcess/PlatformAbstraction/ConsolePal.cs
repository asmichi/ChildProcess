// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.PlatformAbstraction
{
    internal interface IConsolePal
    {
        SafeFileHandle? CreateStdInputHandleForChild(bool createNewConsole);
        SafeFileHandle? CreateStdOutputHandleForChild(bool createNewConsole);
        SafeFileHandle? CreateStdErrorHandleForChild(bool createNewConsole);
        bool HasConsoleWindow();
    }

    internal static class ConsolePal
    {
        private static readonly IConsolePal Impl = CreatePlatformSpecificImpl();

        private static IConsolePal CreatePlatformSpecificImpl()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => new Windows.WindowsConsolePal(),
                PlatformKind.Unix => new Unix.UnixConsolePal(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        /// <summary>
        /// Creates a duplicate handle to the stdin of the current process that can be inherited by a child process.
        /// </summary>
        /// <returns>
        /// The created handle. <see langword="null"/> if such an inheritable handle cannot be created.
        /// </returns>
        public static SafeFileHandle? CreateStdInputHandleForChild(bool createNewConsole) => Impl.CreateStdInputHandleForChild(createNewConsole);
        public static SafeFileHandle? CreateStdOutputHandleForChild(bool createNewConsole) => Impl.CreateStdOutputHandleForChild(createNewConsole);
        public static SafeFileHandle? CreateStdErrorHandleForChild(bool createNewConsole) => Impl.CreateStdErrorHandleForChild(createNewConsole);
        public static bool HasConsoleWindow() => Impl.HasConsoleWindow();
    }
}
