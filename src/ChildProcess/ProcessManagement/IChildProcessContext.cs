// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

namespace Asmichi.Utilities.ProcessManagement
{
    internal interface IChildProcessContext
    {
        IChildProcessStateHolder SpawnProcess(
            ref ChildProcessStartInfoInternal startInfo,
            string resolvedPath,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr);
    }
}
