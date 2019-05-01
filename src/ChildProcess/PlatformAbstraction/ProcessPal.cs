// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal static class ProcessPal
    {
        public static SafeProcessHandle SpawnProcess(
            string fileName,
            IReadOnlyCollection<string> arguments,
            string workingDirectory,
            IReadOnlyCollection<(string name, string value)> environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.ProcessPalWindows.SpawnProcess(fileName, arguments, workingDirectory, environmentVariables, stdIn, stdOut, stdErr);
                case PlatformKind.Linux:
                    return Linux.ProcessPalLinux.SpawnProcess(fileName, arguments, workingDirectory, environmentVariables, stdIn, stdOut, stdErr);
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
