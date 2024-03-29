// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;
using Asmichi.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
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
    /// <list type="bullet">
    /// <item>Modifies the state of a child process.</item>
    /// <item>Detects changes in the states of child processes.</item>
    /// </list>
    /// </summary>
    // NOTE: A pipe to a process itself is not a part of the state of a child process (but of ours).
    internal interface IChildProcessState
    {
        int ProcessId { get; }
        int ExitCode { get; }
        WaitHandle ExitedWaitHandle { get; }
        bool HasExitCode { get; }

        // Pre: The process has exited
        void DangerousRetrieveExitCode();

        bool HasHandle { get; }
        SafeProcessHandle ProcessHandle { get; }
        SafeThreadHandle PrimaryThreadHandle { get; }

        bool CanSignal { get; }
        void SignalInterrupt();
        void SignalTermination();
        void Kill();
    }
}
