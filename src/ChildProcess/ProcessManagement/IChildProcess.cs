// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    // Not providing `Handle` since *nix does not provide a file descriptor of a process
    // (except for a pidfd in Linux).
    //
    // "everything is a file descriptor or a process" --- Linus Torvalds
    // https://lore.kernel.org/lkml/Pine.LNX.4.44.0206091056550.13459-100000@home.transmeta.com/

    /// <summary>
    /// <para>
    /// Provides methods for accessing a child-process-like object.
    /// All members are not thread safe and must not be called simultaneously by multiple threads.
    /// </para>
    /// <para>
    /// If <see cref="CanSignal"/> is <see langword="true"/>, the process will eventually receive the termination signal after this instance has been disposed.
    /// If <see cref="CanSignal"/> is <see langword="false"/>, the disposal behavior varies across platforms.
    /// </para>
    /// </summary>
    public interface IChildProcess : IDisposable
    {
        /// <summary>
        /// Gets the system-generated process identifier for the process.
        /// </summary>
        /// <remarks>
        /// On *nix, the identifier will get invalid as soon as the process exits.
        /// Make sure to keep the process running when using the identifier.
        /// </remarks>
        int Id { get; }

        /// <summary>
        /// <para>Gets the exit code of the process.</para>
        /// <para>(Non-Windows-Specific) If the process was terminated by signal N, the exit code will be -N.</para>
        /// </summary>
        /// <remarks>
        /// (Known Issue) On macOS prior to 11.0, if the process was terminated by a signal, the exit code will always be -1.
        /// </remarks>
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
        /// Gets a value indicating whether <see cref="Handle"/> can be read, that is,
        /// the process was created with <see cref="ChildProcessFlags.EnableHandle"/>.
        /// </summary>
        bool HasHandle { get; }

        /// <summary>
        /// Gets the native handle to the process.
        /// Only available when the process was created with <see cref="ChildProcessFlags.EnableHandle"/>.
        /// </summary>
        /// <remarks>
        /// Do not dispose the returned handle.
        /// </remarks>
        /// <exception cref="NotSupportedException">The process was created without <see cref="ChildProcessFlags.EnableHandle"/>.</exception>
        SafeProcessHandle Handle { get; }

        /// <summary>
        /// (Windows-specific) Gets the native handle to the primary thread of the process.
        /// Only available when the process was created with <see cref="ChildProcessFlags.EnableHandle"/>.
        /// </summary>
        /// <remarks>
        /// Do not dispose the returned handle.
        /// </remarks>
        /// <exception cref="NotSupportedException">The process was created without <see cref="ChildProcessFlags.EnableHandle"/>.</exception>
        /// <exception cref="PlatformNotSupportedException">Not supported on this platform.</exception>
        SafeHandle PrimaryThreadHandle { get; }

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
        /// Waits the specified amount of time for the process to exit.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the process to exit. <see cref="Timeout.InfiniteTimeSpan"/> means infinite amount of time.</param>
        /// <returns>true if the process has exited. Otherwise false.</returns>
        bool WaitForExit(TimeSpan timeout);

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
        /// Asynchronously waits the specified amount of time for the process to exit.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the process to exit. <see cref="Timeout.InfiniteTimeSpan"/> means infinite amount of time.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation. true if the process has exited. Otherwise false.</returns>
        Task<bool> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether this instance supports sending signals (was created without <see cref="ChildProcessFlags.AttachToCurrentConsole"/>).
        /// </summary>
        bool CanSignal { get; }

        /// <summary>
        /// <para>Sends the interrupt signal to the process group. Succeeds if the process has already exited.</para>
        /// <para>(Windows-specific) Sends Ctrl+C to the pseudo console.</para>
        /// <para>(Non-Windows-specific) Sends SIGINT to the process group.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance does not support sending signals (<see cref="CanSignal"/> is <see langword="false"/>).</exception>
        void SignalInterrupt();

        /// <summary>
        /// <para>Sends the termination signal to the process group. Succeeds if the process has already exited.</para>
        /// <para>
        /// (Windows-specific) Closes the pseudo console and the process will receive the CTRL_CLOSE_EVENT event.
        /// Non-console processes are not currenyly supported. NOTE: See the known issues for an issue on Windows 10 1809 (including Windows Server 2019).
        /// </para>
        /// <para>(Non-Windows-specific) Sends SIGTERM to the process group.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance does not support sending signals (<see cref="CanSignal"/> is <see langword="false"/>).</exception>
        void SignalTermination();

        /// <summary>
        /// <para>Forcibly kills the process group or the process. Succeeds if the process has already exited.</para>
        /// <para>
        /// (Windows-specific) Calls TerminateJobObject with exit code -1 on the job object associated to the process tree,
        /// which kills each process in the process tree unless it broke away from the job object using CREATE_BREAKAWAY_FROM_JOB.
        /// </para>
        /// <para>
        /// (Non-Windows-specific) Sends SIGKILL to the process group or the process.
        /// (A new process group is created if and only if <see cref="ChildProcessFlags.AttachToCurrentConsole"/> is unset).</para>
        /// </summary>
        void Kill();
    }
}
