// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.Serialization;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Thrown when starting a child process is blocked because it may execute an unexpected program.
    /// </summary>
    /// <remarks>
    /// <para>
    /// (Windows-specific) Thrown when <see cref="ChildProcessStartInfo.FileName"/> is "cmd.exe" (without the directory part),
    /// but <see cref="ChildProcessFlags.DisableArgumentQuoting"/> is not set.<br/>
    /// If arguments originate from an untrusted input, it may be a good idea to avoid executing "cmd.exe".
    /// Incorrectly escaped arguments can lead to execution of an arbitrary executable because "cmd.exe /c" takes
    /// an arbitrary shell command line. Search "BatBadBut vulnerability" for the background.<br/>
    /// If arguments are trusted, set <see cref="ChildProcessFlags.DisableArgumentQuoting"/> and escape arguments on your own.
    /// Note you need to escape arguments in a way specific to the command being invoked. There is no standard quoting method possible for "cmd.exe".
    /// </para>
    /// <para>
    /// (Windows-specific) Thrown when <see cref="ChildProcessStartInfo.FileName"/> has the ".bat" or ".cmd" extension.
    /// Supply the batch file to "cmd.exe /c" instead. See also the above paragraph.
    /// </para>
    /// </remarks>
    [Serializable]
    public class ChildProcessStartingBlockedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartingBlockedException"/> class.
        /// </summary>
        public ChildProcessStartingBlockedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartingBlockedException"/> class
        /// with a message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ChildProcessStartingBlockedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartingBlockedException"/> class
        /// with a message and an inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ChildProcessStartingBlockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartingBlockedException"/> class with serialized data.
        /// </summary>
        /// <param name="info"><see cref="SerializationInfo"/>.</param>
        /// <param name="context"><see cref="StreamingContext"/>.</param>
        protected ChildProcessStartingBlockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
