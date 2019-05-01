// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Specifies how a stdin is redirected.
    /// </summary>
    public enum InputRedirection
    {
        /// <summary>
        /// Redirected to the stdin of the current process.
        /// </summary>
        ParentInput,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the StandardInput property.
        /// </summary>
        InputPipe,

        /// <summary>
        /// Redirected to a file. The corresponding StdInputFile property must be set.
        /// </summary>
        File,

        /// <summary>
        /// Redirected to a file handle. The corresponding StdInputHandle property must be set.
        /// </summary>
        Handle,

        /// <summary>
        /// Redirected to the null device: NUL on Windows, /dev/null on *nix.
        /// </summary>
        NullDevice,
    }

    /// <summary>
    /// Specifies how a stdout or a stderr is redirected.
    /// </summary>
    public enum OutputRedirection
    {
        /// <summary>
        /// Redirected to the stdout of the current process.
        /// </summary>
        ParentOutput,

        /// <summary>
        /// Redirected to the stderr of the current process.
        /// </summary>
        ParentError,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the StandardOutput property.
        /// </summary>
        OutputPipe,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the StandardError property.
        /// </summary>
        ErrorPipe,

        /// <summary>
        /// Redirected to a file. The existing content of the file will be truncated. The corresponding StdOutputFile or StdErrorFile property must be set.
        /// </summary>
        File,

        /// <summary>
        /// Redirected to a file. New bytes written will be appended to the file. The corresponding StdOutputFile or StdErrorFile property must be set.
        /// </summary>
        AppendToFile,

        /// <summary>
        /// Redirected to a file handle. The corresponding StdOutputtHandle or StdErrorHandle property must be set.
        /// </summary>
        Handle,

        /// <summary>
        /// Redirected to the null device: NUL on Windows, /dev/null on *nix.
        /// </summary>
        NullDevice,
    }

    /// <summary>
    /// Specifies parameters that are used to start a child process.
    /// </summary>
    public sealed class ChildProcessStartInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartInfo"/> class.
        /// The FileName and Arguments properties must be set later.
        /// </summary>
        public ChildProcessStartInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessStartInfo"/> class with the specified command.
        /// </summary>
        /// <param name="fileName">Path to the executable to start.</param>
        /// <param name="arguments">The command-line arguments to be passed to the child process.</param>
        public ChildProcessStartInfo(string fileName, params string[] arguments)
        {
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Path to the executable to start.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The command-line arguments to be passed to the child process.
        /// null will be treated as Array.Empty&lt;string&gt;().
        /// </summary>
        public IReadOnlyCollection<string> Arguments { get; set; }

        /// <summary>
        /// The working directory of the child process.
        /// If it is null, the child process inherits the working directory of the current process.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The list of the environment variables that apply to the child process.
        /// If it is null, the child process inherits the environment variables of the current process.
        /// </summary>
        public IReadOnlyCollection<(string name, string value)> EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies how the stdin of the child process is redirected.
        /// </summary>
        public InputRedirection StdInputRedirection { get; set; } = InputRedirection.NullDevice;

        /// <summary>
        /// Specifies how the stdout of the child process is redirected.
        /// The default value is <see cref="OutputRedirection.ParentOutput"/>.
        /// </summary>
        public OutputRedirection StdOutputRedirection { get; set; } = OutputRedirection.ParentOutput;

        /// <summary>
        /// Specifies how the stderr of the child process is redirected.
        /// The default value is <see cref="OutputRedirection.ParentError"/>.
        /// </summary>
        public OutputRedirection StdErrorRedirection { get; set; } = OutputRedirection.ParentError;

        /// <summary>
        /// If <see cref="StdInputRedirection"/> is <see cref="InputRedirection.File"/>,
        /// specifies the file where the stdin of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdInputFile { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stdout of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdOutputFile { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stderr of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdErrorFile { get; set; }

        /// <summary>
        /// If <see cref="StdInputRedirection"/> is <see cref="InputRedirection.Handle"/>,
        /// specifies the file handle where the stdin of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdInputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stdout of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdOutputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stderr of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdErrorHandle { get; set; }
    }
}
