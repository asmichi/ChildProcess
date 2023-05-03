// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities;
using Microsoft.Win32.SafeHandles;
using static Asmichi.Utilities.ArgumentValidationUtil;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Represents a child process created.
    /// Static members are thread-safe.
    /// All instance members are not thread-safe and must not be called simultaneously by multiple threads.
    /// </summary>
    internal sealed class ChildProcessImpl : IChildProcess, IDisposable
    {
        private readonly IChildProcessStateHolder _stateHolder;
        private readonly Stream? _standardInput;
        private readonly Stream? _standardOutput;
        private readonly Stream? _standardError;
        private bool _isDisposed;

        internal ChildProcessImpl(
            IChildProcessStateHolder childProcessStateHolder,
            Stream? standardInput,
            Stream? standardOutput,
            Stream? standardError)
        {
            _stateHolder = childProcessStateHolder;
            _standardInput = standardInput;
            _standardOutput = standardOutput;
            _standardError = standardError;
        }

        /// <summary>
        /// Releases resources associated to this object.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stateHolder.Dispose();
                _standardInput?.Dispose();
                _standardOutput?.Dispose();
                _standardError?.Dispose();

                _isDisposed = true;
            }
        }

        public bool HasStandardInput => _standardInput is not null;
        public bool HasStandardOutput => _standardOutput is not null;
        public bool HasStandardError => _standardError is not null;

        public Stream StandardInput => _standardInput ?? throw new InvalidOperationException("No StandardInput associated.");
        public Stream StandardOutput => _standardOutput ?? throw new InvalidOperationException("No StandardOutput associated.");
        public Stream StandardError => _standardError ?? throw new InvalidOperationException("No StandardError associated.");

        /// <summary>
        /// (For tests.) Tests use this to wait for the child process without caching its status.
        /// </summary>
        internal WaitHandle ExitedWaitHandle => _stateHolder.State.ExitedWaitHandle;

        public void WaitForExit() => WaitForExit(Timeout.InfiniteTimeSpan);

        public bool WaitForExit(int millisecondsTimeout) => WaitForExit(TimeSpan.FromMilliseconds(ValidateTimeoutRange(millisecondsTimeout)));

        public bool WaitForExit(TimeSpan timeout)
        {
            ValidateTimeoutRange(timeout);
            CheckNotDisposed();

            var state = _stateHolder.State;

            if (state.HasExitCode)
            {
                return true;
            }

            if (!state.ExitedWaitHandle.WaitOne(timeout))
            {
                return false;
            }

            state.DangerousRetrieveExitCode();
            return true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            WaitForExitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        public Task<bool> WaitForExitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default) =>
            WaitForExitAsync(TimeSpan.FromMilliseconds(ValidateTimeoutRange(millisecondsTimeout)), cancellationToken);

        public Task<bool> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateTimeoutRange(timeout);
            CheckNotDisposed();

            var state = _stateHolder.State;

            if (state.HasExitCode)
            {
                return CompletedBoolTask.True;
            }

            // Synchronous path: the process has already exited.
            var waitHandle = _stateHolder.State.ExitedWaitHandle;
            if (waitHandle.WaitOne(0))
            {
                state.DangerousRetrieveExitCode();
                return CompletedBoolTask.True;
            }

            // Synchronous path: already canceled.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(cancellationToken);
            }

            // Start an asynchronous wait operation.
            var operation = WaitAsyncOperation.Start(waitHandle, timeout, cancellationToken);
            return operation.Completion;
        }

        public int Id
        {
            get
            {
                CheckNotDisposed();

                return _stateHolder.State.ProcessId;
            }
        }

        public int ExitCode
        {
            get
            {
                CheckNotDisposed();
                RetrieveExitCode();

                return _stateHolder.State.ExitCode;
            }
        }

        public bool HasHandle
        {
            get
            {
                CheckNotDisposed();
                return _stateHolder.State.HasHandle;
            }
        }

        public SafeProcessHandle Handle
        {
            get
            {
                CheckNotDisposed();
                return _stateHolder.State.ProcessHandle;
            }
        }

        public SafeHandle PrimaryThreadHandle
        {
            get
            {
                CheckNotDisposed();
                return _stateHolder.State.PrimaryThreadHandle;
            }
        }

        public bool CanSignal
        {
            get
            {
                CheckNotDisposed();
                return _stateHolder.State.CanSignal;
            }
        }

        public void SignalInterrupt()
        {
            CheckNotDisposed();
            CheckCanSignal();

            _stateHolder.State.SignalInterrupt();
        }

        public void SignalTermination()
        {
            CheckNotDisposed();
            CheckCanSignal();

            _stateHolder.State.SignalTermination();
        }

        public void Kill()
        {
            CheckNotDisposed();

            _stateHolder.State.Kill();
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChildProcessImpl));
            }
        }

        private void CheckCanSignal()
        {
            if (!_stateHolder.State.CanSignal)
            {
                throw new InvalidOperationException("This instance does not support sending signals.");
            }
        }

        private void RetrieveExitCode()
        {
            if (!_stateHolder.State.HasExitCode)
            {
                if (!WaitForExit(TimeSpan.Zero))
                {
                    throw new InvalidOperationException("The process has not exited. Call WaitForExit before accessing ExitCode.");
                }

                _stateHolder.State.DangerousRetrieveExitCode();
            }
        }
    }
}
