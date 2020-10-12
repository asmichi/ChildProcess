// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Specifies how a child process is created.
    /// </summary>
    [Flags]
    public enum ChildProcessFlags
    {
        /// <summary>
        /// Specifies that no options are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies that the search path should not be searched for the executable.
        /// See <see cref="ChildProcessStartInfo.FileName"/> for details.
        /// </summary>
        IgnoreSearchPath = 0x0001,

        /// <summary>
        /// Specifies that <see cref="ChildProcessStartInfo.FileName"/> is treated as
        /// a path relative to the current directory (if it is not an absolute path).
        /// See <see cref="ChildProcessStartInfo.FileName"/> for details.
        /// </summary>
        AllowRelativeFileName = 0x0002,
    }

    /// <summary>
    /// Provides extension methos for <see cref="ChildProcessFlags"/>.
    /// </summary>
    public static class ChildProcessFlagsExtensions
    {
        /// <summary>
        /// Returns whether <paramref name="flags"/> has the <see cref="ChildProcessFlags.IgnoreSearchPath"/> flag.
        /// </summary>
        /// <param name="flags">The <see cref="ChildProcessFlags"/> to inspect.</param>
        /// <returns><see langword="true"/> if <paramref name="flags"/> has the <see cref="ChildProcessFlags.IgnoreSearchPath"/> flag.</returns>
        public static bool HasIgnoreSearchPath(this ChildProcessFlags flags) => (flags & ChildProcessFlags.IgnoreSearchPath) != 0;

        /// <summary>
        /// Returns whether <paramref name="flags"/> has the <see cref="ChildProcessFlags.AllowRelativeFileName"/> flag.
        /// </summary>
        /// <param name="flags">The <see cref="ChildProcessFlags"/> to inspect.</param>
        /// <returns><see langword="true"/> if <paramref name="flags"/> has the <see cref="ChildProcessFlags.AllowRelativeFileName"/> flag.</returns>
        public static bool HasSearchCurrentDirectory(this ChildProcessFlags flags) => (flags & ChildProcessFlags.AllowRelativeFileName) != 0;
    }

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
        /// Redirected to a file handle. The corresponding StdOutputHandle or StdErrorHandle property must be set.
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
            FileName = fileName;
            Arguments = arguments;
        }

        /// <summary>
        /// <para>Path to the executable to start.</para>
        /// <para>
        /// The executable is searched for in the following order:
        /// <list type="number">
        /// <item>
        /// If <see cref="ChildProcessFlags.AllowRelativeFileName"/> is set, <see cref="FileName"/> is
        /// treated as a path relative to the current directory.
        /// </item>
        /// <item>
        /// If <see cref="ChildProcessFlags.IgnoreSearchPath"/> is unset and <see cref="FileName"/> does not contain
        /// any path separators, the directories specified in <see cref="SearchPath"/> are searched for the executable.
        /// </item>
        /// </list>
        /// </para>
        /// Note that unlike <see cref="Process.Start(ProcessStartInfo)"/> this procedure does not search the directory of the current executable.
        /// <para>
        /// (Windows-specific) If <see cref="FileName"/> does not contain an extension, ".exe" is appended.
        /// </para>
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// <para>The command-line arguments to be passed to the child process. The default value is the empty array.</para>
        /// </summary>
        public IReadOnlyCollection<string> Arguments { get; set; } = Array.Empty<string>();

        /// <summary>
        /// <para>The working directory of the child process. The default value is <see langword="null"/>.</para>
        /// <para>If it is null, the child process inherits the working directory of the current process.</para>
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// <para>
        /// The list of the environment variables that apply to the child process.
        /// The default value is <see langword="null"/>.
        /// </para>
        /// <para>If it is null, the child process inherits the environment variables of the current process.</para>
        /// </summary>
        public IReadOnlyCollection<KeyValuePair<string, string>>? EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies how the child process is created.
        /// The default value is <see cref="ChildProcessFlags.None"/>.
        /// </summary>
        public ChildProcessFlags Flags { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the directories to be searched for the executable.
        /// The default value is <see langword="null"/>.
        /// </para>
        /// <para>
        /// If it is <see langword="null"/>, the directories specified in the PATH environment variable are used as the defaut.
        /// (Windows-specific) This default list has the 32-bit Windows system directory (system32) and the Windows directory prepended.
        /// </para>
        /// </summary>
        public IReadOnlyList<string>? SearchPath { get; set; }

        /// <summary>
        /// Specifies how the stdin of the child process is redirected.
        /// The default value is <see cref="InputRedirection.NullDevice"/>.
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
        public string? StdInputFile { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stdout of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public string? StdOutputFile { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stderr of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public string? StdErrorFile { get; set; }

        /// <summary>
        /// If <see cref="StdInputRedirection"/> is <see cref="InputRedirection.Handle"/>,
        /// specifies the file handle where the stdin of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle? StdInputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stdout of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle? StdOutputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stderr of the child process is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle? StdErrorHandle { get; set; }
    }
}
