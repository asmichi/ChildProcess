// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.Utilities.Interop.Linux;

namespace Asmichi.Utilities.PlatformAbstraction.Unix
{
    internal sealed class UnixEnvironmentPal : IEnvironmentPal
    {
        private static readonly int ENOENT = LibChildProcess.GetENOENT();

        public char SearchPathSeparator { get; } = ':';

        public bool IsFileNotFoundError(int error) => error == ENOENT;
    }
}
