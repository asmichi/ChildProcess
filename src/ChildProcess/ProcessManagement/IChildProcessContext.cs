// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.ProcessManagement
{
    internal interface IChildProcessContext
    {
        IChildProcessStateHolder SpawnProcess(
            string fileName,
            IReadOnlyCollection<string> arguments,
            string? workingDirectory,
            IReadOnlyCollection<KeyValuePair<string, string>>? environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr);
    }
}
