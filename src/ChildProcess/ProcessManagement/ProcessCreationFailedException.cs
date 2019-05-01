// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.Serialization;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Thrown when creation of a child process failed.
    /// </summary>
    [Serializable]
    public class ProcessCreationFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCreationFailedException"/> class.
        /// </summary>
        public ProcessCreationFailedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCreationFailedException"/> class
        /// with a message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public ProcessCreationFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCreationFailedException"/> class
        /// with a message and an inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProcessCreationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCreationFailedException"/> class with serialized data.
        /// </summary>
        /// <param name="info"><see cref="SerializationInfo"/>.</param>
        /// <param name="context"><see cref="StreamingContext"/>.</param>
        protected ProcessCreationFailedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
