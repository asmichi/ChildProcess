// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// We know WaitAsyncOperation objects will not be exposed.
#pragma warning disable CA2002 // Do not lock on objects with weak identity

namespace Asmichi.ProcessManagement
{
    // Creates an asynchronous operation that performs WaitHandle.WaitOne and cleans itself up.
    internal sealed class WaitAsyncOperation
    {
        private static readonly WaitOrTimerCallback CachedWaitForExitCompletedDelegate = WaitForExitCompleted;
        private static readonly Action<object?> CachedWaitForExitCanceledDelegate = WaitForExitCanceled;

        private readonly TaskCompletionSource<bool> _completionSource;
        private RegisteredWaitHandle _waitRegistration = null!;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public WaitAsyncOperation()
        {
            // For safety, run continuations of _completionSource.Task outside the callback
            // so they will not block the thread running the callback (especially CTS.Cancel).
            _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<bool> Completion => _completionSource.Task;

        public static WaitAsyncOperation Start(WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var value = new WaitAsyncOperation();
            value.StartImpl(waitHandle, millisecondsTimeout, cancellationToken);
            return value;
        }

        private void StartImpl(WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            lock (this)
            {
                _waitRegistration = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle, CachedWaitForExitCompletedDelegate, this, millisecondsTimeout, executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(
                        CachedWaitForExitCanceledDelegate, this, useSynchronizationContext: false);
                }
            }
        }

        // NOTE: This callback is called synchronously from CTS.Cancel().
        private static void WaitForExitCanceled(object? state)
        {
            Debug.Assert(state != null);

            // Ensure that all writes made by StartAsync are visible.
            lock (state)
            {
            }

            var self = (WaitAsyncOperation)state;

            self.ReleaseResources();
            self._completionSource.TrySetCanceled();
        }

        // NOTE: This callback is executed on a thread-pool thread.
        private static void WaitForExitCompleted(object? state, bool timedOut)
        {
            Debug.Assert(state != null);

            // Ensure that all writes made by StartAsync are visible.
            lock (state)
            {
            }

            var self = (WaitAsyncOperation)state;

            self.ReleaseResources();
            // Not calling parent.DangerousRetrieveExitCode here. It would require some memory barrier.
            self._completionSource.TrySetResult(!timedOut);
        }

        private void ReleaseResources()
        {
            if (_cancellationTokenRegistration != default)
            {
                lock (this)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            // RegisteredWaitHandle is thread-safe.
            _waitRegistration.Unregister(null);
        }
    }
}
