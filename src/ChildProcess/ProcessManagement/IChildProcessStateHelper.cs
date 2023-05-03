// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// <list type="bullet">
    /// <item>Spawns child processes.</item>
    /// <item>Modifies the state of a child process.</item>
    /// <item>Detects changes in the states of child processes.</item>
    /// </list>
    /// </summary>
    internal interface IChildProcessStateHelper : IDisposable
    {
        void ValidatePlatformSpecificStartInfo(
            in ChildProcessStartInfoInternal startInfo);

        IChildProcessStateHolder SpawnProcess(
            ref ChildProcessStartInfoInternal startInfo,
            string resolvedPath,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr);
    }
}
