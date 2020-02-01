// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal static class ConsolePal
    {
        public static SafeFileHandle GetStdInputHandleForChild()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.ConsolePalWindows.GetStdInputHandleForChild(),
                PlatformKind.Linux => Linux.ConsolePalLinux.GetStdInputHandleForChild(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static SafeFileHandle GetStdOutputHandleForChild()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.ConsolePalWindows.GetStdOutputHandleForChild(),
                PlatformKind.Linux => Linux.ConsolePalLinux.GetStdOutputHandleForChild(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static SafeFileHandle GetStdErrorHandleForChild()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.ConsolePalWindows.GetStdErrorHandleForChild(),
                PlatformKind.Linux => Linux.ConsolePalLinux.GetStdErrorHandleForChild(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static bool HasConsoleWindow()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.ConsolePalWindows.HasConsoleWindow(),
                PlatformKind.Linux => Linux.ConsolePalLinux.HasConsoleWindow(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }
    }
}
