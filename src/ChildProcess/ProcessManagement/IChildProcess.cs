// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Provides methods for accessing a child-process-like object.
    /// All members are not thread safe and must not be called simultaneously by multiple threads.
    /// </summary>
    public interface IChildProcess : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the exit code of the process is 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        bool IsSuccessful { get; }

        /// <summary>
        /// <para>Gets the exit code of the process.</para>
        /// <para>(Non-Windows-Specific) If the process was terminated by signal N, the exit code will be -N.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        int ExitCode { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="StandardInput"/> has a value.
        /// </summary>
        bool HasStandardInput { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="StandardOutput"/> has a value.
        /// </summary>
        bool HasStandardOutput { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="StandardError"/> has a value.
        /// </summary>
        bool HasStandardError { get; }

        /// <summary>
        /// A stream associated to the pipe that writes to the stdin of the process.
        /// If no such pipe has been created (<see cref="HasStandardInput"/> is <see langword="false"/>), throws <see cref="InvalidOperationException"/>.
        /// </summary>
        Stream StandardInput { get; }

        /// <summary>
        /// A stream associated to the pipe that reads from the stdout of the process.
        /// If no such pipe has been created (<see cref="HasStandardOutput"/> is <see langword="false"/>), throws <see cref="InvalidOperationException"/>.
        /// </summary>
        Stream StandardOutput { get; }

        /// <summary>
        /// Gets a stream associated to the pipe that reads from the stderr of the process.
        /// If no such pipe has been created (<see cref="HasStandardError"/> is <see langword="false"/>), throws <see cref="InvalidOperationException"/>.
        /// </summary>
        Stream StandardError { get; }

        /// <summary>
        /// Waits indefinitely for the process to exit.
        /// </summary>
        void WaitForExit();

        /// <summary>
        /// Waits <paramref name="millisecondsTimeout"/> milliseconds for the process to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the process to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <returns>true if the process has exited. Otherwise false.</returns>
        bool WaitForExit(int millisecondsTimeout);

        /// <summary>
        /// Asynchronously waits indefinitely for the process to exit.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
        Task WaitForExitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously waits <paramref name="millisecondsTimeout"/> milliseconds for the process to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the process to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation. true if the process has exited. Otherwise false.</returns>
        Task<bool> WaitForExitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether this instance supports sending signals (was created without <see cref="ChildProcessFlags.AttachToCurrentConsole"/>).
        /// </summary>
        bool CanSignal { get; }

        /// <summary>
        /// <para>Sends the interrupt signal to the process group. Succeeds if the process has already exited.</para>
        /// <para>(Windows-specific) Sends Ctrl+C to the pseudo console.</para>
        /// <para>(Non-Windows-specific) Sends SIGKILL to the process group.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance does not support sending signals (<see cref="CanSignal"/> is <see langword="false"/>).</exception>
        void SignalInterrupt();

        /// <summary>
        /// <para>Sends the termination signal to the process group. Succeeds if the process has already exited.</para>
        /// <para>
        /// (Windows-specific) Closes the pseudo console and the process will receive the CTRL_CLOSE_EVENT event.
        /// Non-console processes are not currenyly supported.
        /// </para>
        /// <para>(Non-Windows-specific) Sends SIGTERM to the process group.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance does not support sending signals (<see cref="CanSignal"/> is <see langword="false"/>).</exception>
        void SignalTermination();

        /// <summary>
        /// <para>Forcibly kill the process group. Succeeds if the process has already exited.</para>
        /// <para>(Windows-specific) Calls TerminateProcess on the process with exit code -1.</para>
        /// <para>(Non-Windows-specific) Sends SIGKILL to the process group.</para>
        /// </summary>
        void Kill();
    }
}
