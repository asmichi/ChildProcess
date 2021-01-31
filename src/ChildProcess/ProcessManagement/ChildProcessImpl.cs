// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities.Utilities;

namespace Asmichi.Utilities.ProcessManagement
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

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdInputRedirection"/> set to <see cref="InputRedirection.InputPipe"/>,
        /// <see langword="true"/>.
        /// Otherwise <see langword="false"/>.
        /// </summary>
        public bool HasStandardInput => _standardInput is { };

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ChildProcessStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.OutputPipe"/>, <see langword="true"/>.
        /// Otherwise <see langword="false"/>.
        /// </summary>
        public bool HasStandardOutput => _standardOutput is { };

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ChildProcessStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.ErrorPipe"/>, <see langword="true"/>.
        /// Otherwise <see langword="false"/>.
        /// </summary>
        public bool HasStandardError => _standardError is { };

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdInputRedirection"/> set to <see cref="InputRedirection.InputPipe"/>,
        /// a stream assosiated to that pipe.
        /// Otherwise throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public Stream StandardInput => _standardInput ?? throw new InvalidOperationException("No StandardInput associated.");

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ChildProcessStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.OutputPipe"/>, a stream assosiated to that pipe.
        /// Otherwise throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public Stream StandardOutput => _standardOutput ?? throw new InvalidOperationException("No StandardOutput associated.");

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ChildProcessStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.ErrorPipe"/>, a stream assosiated to that pipe.
        /// Otherwise throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public Stream StandardError => _standardError ?? throw new InvalidOperationException("No StandardError associated.");

        /// <summary>
        /// (For tests.) Tests use this to wait for the child process without caching its status.
        /// </summary>
        internal WaitHandle ExitedWaitHandle => _stateHolder.State.ExitedWaitHandle;

        /// <summary>
        /// Waits indefinitely for the process to exit.
        /// </summary>
        public void WaitForExit() => WaitForExit(Timeout.Infinite);

        /// <summary>
        /// Waits <paramref name="millisecondsTimeout"/> milliseconds for the process to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the process to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <returns>true if the process has exited. Otherwise false.</returns>
        public bool WaitForExit(int millisecondsTimeout)
        {
            ArgumentValidationUtil.CheckTimeOutRange(millisecondsTimeout);
            CheckNotDisposed();

            var state = _stateHolder.State;

            if (state.HasExitCode)
            {
                return true;
            }

            if (!state.ExitedWaitHandle.WaitOne(millisecondsTimeout))
            {
                return false;
            }

            state.DangerousRetrieveExitCode();
            return true;
        }

        /// <summary>
        /// Asynchronously waits indefinitely for the process to exit.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            WaitForExitAsync(Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Asynchronously waits <paramref name="millisecondsTimeout"/> milliseconds for the process to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the process to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation. true if the process has exited. Otherwise false.</returns>
        public Task<bool> WaitForExitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            ArgumentValidationUtil.CheckTimeOutRange(millisecondsTimeout);
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
            var operation = WaitAsyncOperation.Start(waitHandle, millisecondsTimeout, cancellationToken);
            return operation.Completion;
        }

        /// <summary>
        /// Gets if the exit code of the process is 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        public bool IsSuccessful => ExitCode == 0;

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        public int ExitCode
        {
            get
            {
                CheckNotDisposed();
                RetrieveExitCode();

                return _stateHolder.State.ExitCode;
            }
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChildProcessImpl));
            }
        }

        private void RetrieveExitCode()
        {
            if (!_stateHolder.State.HasExitCode)
            {
                if (!WaitForExit(0))
                {
                    throw new InvalidOperationException("The process has not exited. Call WaitForExit before accessing ExitCode.");
                }

                _stateHolder.State.DangerousRetrieveExitCode();
            }
        }
    }
}
