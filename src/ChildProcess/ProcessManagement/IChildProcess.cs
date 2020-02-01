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
        /// Gets if the exit code of the process is 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        bool IsSuccessful { get; }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        int ExitCode { get; }

        /// <summary>
        /// A stream associated to the pipe that writes to the stdin of the process, if any.
        /// If no such pipe has been crated, null.
        /// </summary>
        Stream? StandardInput { get; }

        /// <summary>
        /// A stream associated to the pipe that reads from the stdout of the process, if any.
        /// If no such pipe has been crated, null.
        /// </summary>
        Stream? StandardOutput { get; }

        /// <summary>
        /// A stream associated to the pipe that reads from the stderr of the process, if any.
        /// If no such pipe has been crated, null.
        /// </summary>
        Stream? StandardError { get; }

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
    }
}
