// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Asmichi.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    internal class WindowsChildProcessState : IChildProcessStateHolder, IChildProcessState
    {
        // The exit code to be passed to TerminateProcess.
        private const int TerminateProcessExitCode = -1;
        // ASCII code of Ctrl+C: 'C' - 0x40
        private const int CtrlCCharacter = 0x03;

        private readonly SafeProcessHandle _processHandle;
        private readonly SafeJobObjectHandle _jobObjectHandle;
        private readonly InputWriterOnlyPseudoConsole? _pseudoConsole;
        private readonly bool _allowSignal;
        private readonly WaitHandle _exitedWaitHandle;
        private int _exitCode = -1;
        private bool _hasExitCode;
        private bool _isPseudoConsoleDisposed;

        public WindowsChildProcessState(
            SafeProcessHandle processHandle,
            SafeJobObjectHandle jobObjectHandle,
            InputWriterOnlyPseudoConsole? pseudoConsole,
            bool allowSignal)
        {
            Debug.Assert(!(allowSignal && pseudoConsole is null));

            _processHandle = processHandle;
            _jobObjectHandle = jobObjectHandle;
            _pseudoConsole = pseudoConsole;
            _allowSignal = allowSignal;
            _exitedWaitHandle = new WindowsProcessWaitHandle(_processHandle);
        }

        public void Dispose()
        {
            _processHandle.Dispose();
            _exitedWaitHandle.Dispose();

            if (!_isPseudoConsoleDisposed)
            {
                _pseudoConsole?.Dispose();
                _isPseudoConsoleDisposed = true;
            }
        }

        public IChildProcessState State => this;
        public int ExitCode => GetExitCode();
        public WaitHandle ExitedWaitHandle => _exitedWaitHandle;
        public bool HasExitCode => _hasExitCode;

        // Pre: The process has exited. Otherwise we will end up getting STILL_ACTIVE (259).
        public void DangerousRetrieveExitCode()
        {
            Debug.Assert(_exitedWaitHandle.WaitOne(0));

            if (!Kernel32.GetExitCodeProcess(_processHandle, out _exitCode))
            {
                throw new Win32Exception();
            }

            _hasExitCode = true;
        }

        private int GetExitCode()
        {
            Debug.Assert(_hasExitCode);
            return _exitCode;
        }

        public bool CanSignal => _allowSignal;

        public unsafe void SignalInterrupt()
        {
            Debug.Assert(_allowSignal);
            Debug.Assert(_pseudoConsole is not null);

            if (_isPseudoConsoleDisposed)
            {
                return;
            }

            fixed (byte* pCtrlC = stackalloc byte[1])
            {
                *pCtrlC = CtrlCCharacter;
                if (!Kernel32.WriteFile(_pseudoConsole.ConsoleInputWriter, pCtrlC, sizeof(byte), out int bytesWritten, null))
                {
                    throw new Win32Exception();
                }
            }
        }

        public void SignalTermination()
        {
            Debug.Assert(_allowSignal);
            Debug.Assert(_pseudoConsole is not null);

            if (_isPseudoConsoleDisposed)
            {
                return;
            }

            if (WindowsVersion.NeedsWorkaroundForWindows1809)
            {
                Kill();
            }

            _pseudoConsole.Dispose();
            _isPseudoConsoleDisposed = true;
        }

        public void Kill()
        {
            if (!Kernel32.TerminateJobObject(_jobObjectHandle, TerminateProcessExitCode))
            {
                throw new Win32Exception();
            }
        }
    }
}
