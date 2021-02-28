// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Thrown when an internal logic error was detected within the Asmichi.ChildProcess library.
    /// </summary>
    [Serializable]
    public class AsmichiChildProcessInternalLogicErrorException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessInternalLogicErrorException"/> class.
        /// </summary>
        public AsmichiChildProcessInternalLogicErrorException()
            : base("Internal logic error.")
        {
            Debug.Fail(Message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessInternalLogicErrorException"/> class
        /// with a message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public AsmichiChildProcessInternalLogicErrorException(string message)
            : base(message)
        {
            Debug.Fail(Message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessInternalLogicErrorException"/> class
        /// with a message and an inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public AsmichiChildProcessInternalLogicErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
            Debug.Fail(Message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmichiChildProcessInternalLogicErrorException"/> class with serialized data.
        /// </summary>
        /// <param name="info"><see cref="SerializationInfo"/>.</param>
        /// <param name="context"><see cref="StreamingContext"/>.</param>
        protected AsmichiChildProcessInternalLogicErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
