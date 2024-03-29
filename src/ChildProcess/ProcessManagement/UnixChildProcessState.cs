// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Asmichi.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Used to count references to a <see cref="UnixChildProcessState"/> object.
    /// Because a <see cref="UnixChildProcessState"/> is shared by the corresponding
    /// <see cref="ChildProcessImpl"/> (disposed by users) and the signal handler,
    /// we need to ensure all references have been dropped before disposing it.
    /// </summary>
    internal sealed class UnixChildProcessStateHolder : IChildProcessStateHolder
    {
        private readonly UnixChildProcessState _state;
        private bool _isDisposed;

        public UnixChildProcessStateHolder(UnixChildProcessState state)
        {
            _state = state;
        }

        ~UnixChildProcessStateHolder()
        {
            if (!Environment.HasShutdownStarted)
            {
                _state.Release();
            }
        }

        public UnixChildProcessState State => _state;
        IChildProcessState IChildProcessStateHolder.State => _state;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _state.Release();
                GC.SuppressFinalize(this);
            }
        }
    }

    internal sealed class UnixChildProcessState : IChildProcessState, IDisposable
    {
        private readonly object _lock = new object();
        private readonly ManualResetEvent _exitedEvent = new ManualResetEvent(false);
        private readonly UnixChildProcessStateHelper _helper;
        private readonly long _token;
        private readonly bool _allowSignal;
        private int _refCount = 1;
        private bool _hasExited;
        private int _processId = -1;
        private int _exitCode = -1;

        private UnixChildProcessState(UnixChildProcessStateHelper helper, long token, bool allowSignal)
        {
            _helper = helper;
            _token = token;
            _allowSignal = allowSignal;
        }

        public int ProcessId => GetProcessId();
        public int ExitCode => GetExitCode();
        public bool HasExitCode => GetHasExited();
        public long Token => _token;
        public WaitHandle ExitedWaitHandle => _exitedEvent;
        public bool HasHandle => false;
        public SafeProcessHandle ProcessHandle => throw new NotSupportedException();
        public SafeThreadHandle PrimaryThreadHandle => throw new NotSupportedException();

        public void Dispose()
        {
            if (CanSignal && !_hasExited)
            {
                // Request asynchronous termination because SengSignal allocates additional resources and may fail.
                // (Dispose should not fail!)
                _helper.RequestAsyncTermination(_token);
            }

            ChildProcessStateCollection.RemoveChildProcessState(this);
            _exitedEvent.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="UnixChildProcessState"/> with a new process token (an identifier unique within the current AssemblyLoadContext).
        /// </summary>
        /// <returns>A <see cref="UnixChildProcessStateHolder"/> that wraps the created <see cref="UnixChildProcessState"/>.</returns>
        public static UnixChildProcessStateHolder Create(UnixChildProcessStateHelper helper, bool allowSignal)
        {
            var state = ChildProcessStateCollection.Create(helper, allowSignal);
            return new UnixChildProcessStateHolder(state);
        }

        /// <summary>
        /// Obtains the <see cref="UnixChildProcessState"/> associated with the specified process token.
        /// </summary>
        /// <param name="token">The process token.</param>
        /// <param name="holder">
        /// When this method returns, contains a <see cref="UnixChildProcessStateHolder"/> that wraps the <see cref="UnixChildProcessState"/>
        /// associated with the specified process token, if the process token is found; otherwise, unspecified.
        /// </param>
        /// <returns><see langword="true"/> if the process token is found; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetChildProcessState(long token, [NotNullWhen(true)] out UnixChildProcessStateHolder? holder)
        {
            if (!ChildProcessStateCollection.TryGetChildProcessState(token, out var state))
            {
                holder = null;
                return false;
            }

            if (!state.TryAddRef())
            {
                // Disposal has already started.
                holder = null;
                return false;
            }

            holder = new UnixChildProcessStateHolder(state);
            return true;
        }

        /// <summary>
        /// Increments the ref count unless disposal has already stared.
        /// </summary>
        /// <returns><see langword="true"/> if this object is still valid; otherwise, <see langword="false"/>.</returns>
        public bool TryAddRef()
        {
            lock (_lock)
            {
                if (_refCount == 0)
                {
                    // Disposal has already started.
                    return false;
                }

                _refCount++;
            }

            return true;
        }

        public void Release()
        {
            bool shouldDispose;
            lock (_lock)
            {
                if (_refCount == 0)
                {
                    throw new AsmichiChildProcessInternalLogicErrorException();
                }

                _refCount--;
                shouldDispose = _refCount == 0;
            }

            if (shouldDispose)
            {
                Dispose();
            }
        }

        private int GetExitCode()
        {
            if (!_hasExited)
            {
                throw new InvalidOperationException("Process has not exited yet.");
            }

            return _exitCode;
        }

        private bool GetHasExited()
        {
            lock (_lock)
            {
                return _hasExited;
            }
        }

        private int GetProcessId()
        {
            Debug.Assert(_processId != -1);
            return _processId;
        }

        /// <summary>
        /// Sets the process ID of the child process.
        /// The caller of <see cref="Create"/> must call this before returning <see cref="UnixChildProcessState"/> to <see cref="ChildProcessImpl"/>.
        /// </summary>
        /// <param name="processId">The process ID of the created child process.</param>
        public void SetProcessId(int processId)
        {
            Debug.Assert(_processId == -1);
            _processId = processId;
        }

        public void SetExited(int exitCode)
        {
            lock (_lock)
            {
                if (_hasExited)
                {
                    throw new AsmichiChildProcessInternalLogicErrorException("Attempted to call SetExited twice.");
                }

                _hasExited = true;
                _exitCode = exitCode;
                _exitedEvent.Set();
            }
        }

        public void DangerousRetrieveExitCode()
        {
            if (!_hasExited)
            {
                throw new AsmichiChildProcessInternalLogicErrorException();
            }

            // SetExited has already set the exit code.
        }

        public bool CanSignal => _allowSignal;

        public void SignalInterrupt()
        {
            Debug.Assert(_allowSignal);
            _helper.SendSignal(_token, UnixHelperProcessSignalNumber.Interrupt);
        }

        public void SignalTermination()
        {
            Debug.Assert(_allowSignal);
            _helper.SendSignal(_token, UnixHelperProcessSignalNumber.Termination);
        }

        public void Kill()
        {
            _helper.SendSignal(_token, UnixHelperProcessSignalNumber.Kill);
        }

        private static class ChildProcessStateCollection
        {
            // AssemblyLoadContext-global because our signal handler would be process-global anyway if we could move our signal handler into the current process.
            private static readonly Dictionary<long, UnixChildProcessState> ChildProcessState = new Dictionary<long, UnixChildProcessState>();
            private static long _prevToken;

            public static UnixChildProcessState Create(UnixChildProcessStateHelper helper, bool allowSignal)
            {
                var token = IssueProcessToken();
                var state = new UnixChildProcessState(helper, token, allowSignal);
                lock (ChildProcessState)
                {
                    ChildProcessState.Add(token, state);
                }
                return state;

                static long IssueProcessToken() => Interlocked.Increment(ref _prevToken);
            }

            public static bool TryGetChildProcessState(long token, [NotNullWhen(true)] out UnixChildProcessState? state)
            {
                lock (ChildProcessState)
                {
                    return ChildProcessState.TryGetValue(token, out state);
                }
            }

            public static void RemoveChildProcessState(UnixChildProcessState childProcessState)
            {
                Debug.Assert(childProcessState._refCount == 0);

                lock (ChildProcessState)
                {
                    ChildProcessState.Remove(childProcessState.Token);
                }
            }
        }
    }
}
