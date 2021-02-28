// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.Interop.Windows;

namespace Asmichi.PlatformAbstraction.Windows
{
    internal sealed class WindowsEnvironmentPal : IEnvironmentPal
    {
        public char SearchPathSeparator { get; } = ';';

        public bool IsFileNotFoundError(int error) => error == Kernel32.ERROR_FILE_NOT_FOUND;
    }
}
