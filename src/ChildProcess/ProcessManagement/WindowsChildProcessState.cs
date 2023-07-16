// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Asmichi.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    internal class WindowsChildProcessState : IChildProcessStateHolder, IChildProcessState
    {
        // The exit code to be passed to TerminateProcess (same as Process.Kill).
        private const int TerminateProcessExitCode = -1;
        // ASCII code of Ctrl+C: 'C' - 0x40
        private const int CtrlCCharacter = 0x03;

        private readonly SafeProcessHandle _processHandle;
        private readonly SafeThreadHandle? _primaryThreadHandle;
        private readonly SafeJobObjectHandle _jobObjectHandle;
        private readonly InputWriterOnlyPseudoConsole? _pseudoConsole;
        private readonly bool _allowSignal;
        private readonly bool _allowHandle;
        private readonly WaitHandle _exitedWaitHandle;
        private readonly int _processId;
        private int _exitCode = -1;
        private bool _hasExitCode;
        private bool _isPseudoConsoleDisposed;

        public WindowsChildProcessState(
            int processId,
            SafeProcessHandle processHandle,
            SafeThreadHandle primaryThreadHandle,
            SafeJobObjectHandle jobObjectHandle,
            InputWriterOnlyPseudoConsole? pseudoConsole,
            bool allowSignal,
            bool allowHandle)
        {
            Debug.Assert(!(allowSignal && pseudoConsole is null));

            _processId = processId;
            _processHandle = processHandle;

            if (allowHandle)
            {
                _primaryThreadHandle = primaryThreadHandle;
            }
            else
            {
                primaryThreadHandle.Dispose();
            }

            _jobObjectHandle = jobObjectHandle;
            _pseudoConsole = pseudoConsole;
            _allowSignal = allowSignal;
            _allowHandle = allowHandle;
            _exitedWaitHandle = new WindowsProcessWaitHandle(_processHandle);
        }

        public void Dispose()
        {
            if (!_isPseudoConsoleDisposed)
            {
                if (WindowsVersion.NeedsWorkaroundForWindows1809)
                {
                    // Should always succeed.
                    Kill();
                }

                // This will terminate the process tree (unless we are on Windows 1809).
                _pseudoConsole?.Dispose();

                _isPseudoConsoleDisposed = true;
            }

            _processHandle.Dispose();
            _primaryThreadHandle?.Dispose();
            _exitedWaitHandle.Dispose();
        }

        public IChildProcessState State => this;
        public int ProcessId => _processId;
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

        public bool HasHandle => _allowHandle;

        public SafeProcessHandle ProcessHandle
        {
            get
            {
                if (!_allowHandle)
                {
                    throw new NotSupportedException();
                }

                return _processHandle;
            }
        }

        public SafeThreadHandle PrimaryThreadHandle
        {
            get
            {
                if (!_allowHandle)
                {
                    throw new NotSupportedException();
                }

                Debug.Assert(_primaryThreadHandle is not null);
                return _primaryThreadHandle;
            }
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
