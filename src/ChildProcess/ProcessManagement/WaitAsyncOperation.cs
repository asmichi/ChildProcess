// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asmichi.Utilities.ProcessManagement
{
    // Creates an asynchronous operation that performs WaitHandle.WaitOne and cleans itself up.
    internal sealed class WaitAsyncOperation
    {
        private static readonly WaitOrTimerCallback CachedWaitForExitCompletedDelegate = WaitForExitCompleted;
        private static readonly Action<object> CachedWaitForExitCanceledDelegate = WaitForExitCanceled;

        private TaskCompletionSource<bool> _completionSource;
        private RegisteredWaitHandle _waitRegistration;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public Task<bool> StartAsync(WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            lock (this)
            {
                // For safety, run continuations of _completionSource.Task outside the callback
                // so they will not block the thread running the callback (especially CTS.Cancel).
                _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                _waitRegistration = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle, CachedWaitForExitCompletedDelegate, this, millisecondsTimeout, executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(
                        CachedWaitForExitCanceledDelegate, this, useSynchronizationContext: false);
                }
            }

            return _completionSource.Task;
        }

        // NOTE: This callback is called synchronously from CTS.Cancel().
        private static void WaitForExitCanceled(object state)
        {
            // Ensure that all writes made by Register are visible.
            lock (state)
            {
            }

            var self = (WaitAsyncOperation)state;

            self.ReleaseResources();
            self._completionSource.TrySetCanceled();
        }

        // NOTE: This callback is executed on a thread-pool thread.
        private static void WaitForExitCompleted(object state, bool timedOut)
        {
            // Ensure that all writes made by Register are visible.
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
