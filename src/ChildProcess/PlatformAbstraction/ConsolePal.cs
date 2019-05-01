// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal static class ConsolePal
    {
        public static SafeFileHandle GetStdInputHandleForChild()
        {
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.ConsolePalWindows.GetStdInputHandleForChild();
                case PlatformKind.Linux:
                    return Linux.ConsolePalLinux.GetStdInputHandleForChild();
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static SafeFileHandle GetStdOutputHandleForChild()
        {
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.ConsolePalWindows.GetStdOutputHandleForChild();
                case PlatformKind.Linux:
                    return Linux.ConsolePalLinux.GetStdOutputHandleForChild();
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static SafeFileHandle GetStdErrorHandleForChild()
        {
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.ConsolePalWindows.GetStdErrorHandleForChild();
                case PlatformKind.Linux:
                    return Linux.ConsolePalLinux.GetStdErrorHandleForChild();
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static bool HasConsoleWindow()
        {
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.ConsolePalWindows.HasConsoleWindow();
                case PlatformKind.Linux:
                    return Linux.ConsolePalLinux.HasConsoleWindow();
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
