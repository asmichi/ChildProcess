// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal static class HandlePal
    {
        public static SafeWaitHandle ToWaitHandle(SafeProcessHandle handle)
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.HandlePalWindows.ToWaitHandle(handle),
                PlatformKind.Linux => Linux.HandlePalLinux.ToWaitHandle(handle),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }
    }
}
