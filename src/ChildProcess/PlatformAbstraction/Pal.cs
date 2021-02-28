// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

// PERF: Not using virtual calls via interfaces so that those calls will be easy to inline.
namespace Asmichi.PlatformAbstraction
{
    internal enum PlatformKind
    {
        Unknown,
        Win32,
        Linux,
    }

    internal static class Pal
    {
        public static readonly PlatformKind PlatformKind = GetPlatformKind();

        private static PlatformKind GetPlatformKind()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return PlatformKind.Win32;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return PlatformKind.Linux;
            }
            // else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            else
            {
                return PlatformKind.Unknown;
            }
        }
    }
}
