// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace Asmichi.PlatformAbstraction
{
    internal interface IEnvironmentPal
    {
        bool IsFileNotFoundError(int error);
        bool IsEnvironmentVariableNameCaseSensitive { get; }
    }

    internal static class EnvironmentPal
    {
        private static readonly IEnvironmentPal Impl = CreatePlatformSpecificImpl();

        private static IEnvironmentPal CreatePlatformSpecificImpl()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => new Windows.WindowsEnvironmentPal(),
                PlatformKind.Unix => new Unix.UnixEnvironmentPal(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static bool IsFileNotFoundError(int error) => Impl.IsFileNotFoundError(error);

        public static bool IsEnvironmentVariableNameCaseSensitive { get; } = Impl.IsEnvironmentVariableNameCaseSensitive;

        public static StringComparison EnvironmentVariableNameComparison { get; } =
            IsEnvironmentVariableNameCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }
}
