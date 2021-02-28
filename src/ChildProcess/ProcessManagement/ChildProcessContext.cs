// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Asmichi.PlatformAbstraction;

namespace Asmichi.ProcessManagement
{
    internal static class ChildProcessContext
    {
        public static IChildProcessContext Shared { get; } = CreateSharedContext();

        private static IChildProcessContext CreateSharedContext() =>
            Pal.PlatformKind switch
            {
                PlatformKind.Win32 => new WindowsChildProcessContext(),
                PlatformKind.Linux => new UnixChildProcessContext(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new AsmichiChildProcessInternalLogicErrorException(),
            };
    }
}
