// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal interface IConsolePal
    {
        SafeFileHandle? GetStdInputHandleForChild();
        SafeFileHandle? GetStdOutputHandleForChild();
        SafeFileHandle? GetStdErrorHandleForChild();
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
                PlatformKind.Linux => new Unix.UnixConsolePal(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static SafeFileHandle? GetStdInputHandleForChild() => Impl.GetStdInputHandleForChild();
        public static SafeFileHandle? GetStdOutputHandleForChild() => Impl.GetStdOutputHandleForChild();
        public static SafeFileHandle? GetStdErrorHandleForChild() => Impl.GetStdErrorHandleForChild();
        public static bool HasConsoleWindow() => Impl.HasConsoleWindow();
    }
}
