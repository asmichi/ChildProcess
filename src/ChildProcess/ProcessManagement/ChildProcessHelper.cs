// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Asmichi.PlatformAbstraction;

namespace Asmichi.ProcessManagement
{
    internal static class ChildProcessHelper
    {
        public static IChildProcessStateHelper Shared { get; } = CreateSharedHelper();

        private static IChildProcessStateHelper CreateSharedHelper() =>
            Pal.PlatformKind switch
            {
                PlatformKind.Win32 => new WindowsChildProcessStateHelper(),
                PlatformKind.Unix => new UnixChildProcessStateHelper(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new AsmichiChildProcessInternalLogicErrorException(),
            };
    }
}
