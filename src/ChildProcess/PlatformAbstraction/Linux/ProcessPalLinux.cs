// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Linux
{
    internal static class ProcessPalLinux
    {
        public static SafeProcessHandle SpawnProcess(
            string fileName,
            IReadOnlyCollection<string> arguments,
            string? workingDirectory,
            IReadOnlyCollection<(string name, string value)>? environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            throw new NotImplementedException();
        }
    }
}
