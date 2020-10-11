// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    internal class WindowsChildProcessState : IChildProcessStateHolder, IChildProcessState
    {
        private readonly SafeProcessHandle _processHandle;
        private readonly WaitHandle _exitedWaitHandle;
        private int _exitCode = -1;
        private bool _hasExitCode;

        public WindowsChildProcessState(SafeProcessHandle processHandle)
        {
            _processHandle = processHandle;
            _exitedWaitHandle = new WindowsProcessWaitHandle(_processHandle);
        }

        public void Dispose()
        {
            _processHandle.Dispose();
            _exitedWaitHandle.Dispose();
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
    }
}
