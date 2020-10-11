// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Reference-counts an <see cref="IChildProcessState"/> instance.
    /// </summary>
    internal interface IChildProcessStateHolder : IDisposable
    {
        IChildProcessState State { get; }
    }

    /// <summary>
    /// Represents the state associated to one process.
    /// </summary>
    internal interface IChildProcessState
    {
        int ExitCode { get; }
        WaitHandle ExitedWaitHandle { get; }
        bool HasExitCode { get; }

        // Pre: The process has exited
        void DangerousRetrieveExitCode();
    }
}
