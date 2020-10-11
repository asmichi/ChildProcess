// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.Serialization;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Thrown when the library (typically the helper process) crashed due to critical disturbance
    /// (the helper was killed, etc.) or a bug.
    /// </summary>
    [Serializable]
    public class AsmichiChildProcessLibraryCrashedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessLibraryCrashedException"/> class.
        /// </summary>
        public AsmichiChildProcessLibraryCrashedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessLibraryCrashedException"/> class
        /// with a message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public AsmichiChildProcessLibraryCrashedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessLibraryCrashedException"/> class
        /// with a message and an inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public AsmichiChildProcessLibraryCrashedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessLibraryCrashedException"/> class with serialized data.
        /// </summary>
        /// <param name="info"><see cref="SerializationInfo"/>.</param>
        /// <param name="context"><see cref="StreamingContext"/>.</param>
        protected AsmichiChildProcessLibraryCrashedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
