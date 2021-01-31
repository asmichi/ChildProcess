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

        /// <summary>
        /// <para>
        /// (Windows-specific) Specifies that newly created consoles should use the code page specified by
        /// <see cref="ChildProcessStartInfo.CodePage"/>. If it is not set, newly created consoles will use
        /// the system default code page.
        /// </para>
        /// <para>Cannot be combined with <see cref="AttachToCurrentConsole"/>.</para>
        /// </summary>
        UseCustomCodePage = 0x0004,

        /// <summary>
        /// <para>
        /// Specifies that the child process should be attached to the current console / session.
        /// If it is set, the child process will be attached the current console / session
        /// and you cannot send the Ctrl+C / SIGINT signal to the process.
        /// If it is not set, the child process will be attached to a new console / session.
        /// </para>
        /// <para>
        /// Avoid this flag if you need to be fully cross-platform; instead always redirect stdin/stdout/stderr of a child process.
        /// This flag is inherently not cross-platform. The behavior of an "attached" process varies between platforms.
        /// </para>
        /// <para>
        /// (Windows-specific) If it is set and the current process is not attached to a console,
        /// the child process will be attached to a pseudo console.
        /// You still cannot send the Ctrl+C signal to the process.
        /// </para>
        /// <para>(Non-Windows-specific) Session creation not implemented yet.</para>
        /// </summary>
        AttachToCurrentConsole = 0x0008,

        /// <summary>
        /// (Windows-specific) Specifies that <see cref="ChildProcessStartInfo.Arguments"/> should not be
        /// automatically quoted, that is, should just be concatenated using a white space (U+0020) as the delimiter.
        /// This effectively gives you full control over the command line.
        /// Note that <see cref="ChildProcessStartInfo.FileName"/> will still be automatically quoted.
        /// </summary>
        DisableArgumentQuoting = 0x0010,
    }

    /// <summary>
    /// Provides extension methos for <see cref="ChildProcessFlags"/>.
    /// </summary>
    internal static class ChildProcessFlagsExtensions
    {
        public static bool HasIgnoreSearchPath(this ChildProcessFlags flags) => (flags & ChildProcessFlags.IgnoreSearchPath) != 0;
        public static bool HasAllowRelativeFileName(this ChildProcessFlags flags) => (flags & ChildProcessFlags.AllowRelativeFileName) != 0;
        public static bool HasUseCustomCodePage(this ChildProcessFlags flags) => (flags & ChildProcessFlags.UseCustomCodePage) != 0;
        public static bool HasAttachToCurrentConsole(this ChildProcessFlags flags) => (flags & ChildProcessFlags.AttachToCurrentConsole) != 0;
        public static bool HasDisableArgumentQuoting(this ChildProcessFlags flags) => (flags & ChildProcessFlags.DisableArgumentQuoting) != 0;
    }

    /// <summary>
    /// Specifies how a stdin is redirected.
    /// </summary>
    public enum InputRedirection
    {
        /// <summary>
        /// <para>Redirected to the stdin of the current process.</para>
        /// <para>
        /// (Windows-specific) If the stdout is not redirected and the child process is not attached to the current console,
        /// redirected to the null device instead.
        /// </para>
        /// </summary>
        ParentInput,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the <see cref="IChildProcess.StandardInput"/> property.
        /// </summary>
        InputPipe,

        /// <summary>
        /// Redirected to a file. <see cref="ChildProcessStartInfo.StdInputFile"/> must also be set.
        /// </summary>
        File,

        /// <summary>
        /// Redirected to a file handle. <see cref="ChildProcessStartInfo.StdInputHandle"/> must also be set.
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
        /// <para>Redirected to the stdout of the current process.</para>
        /// <para>
        /// (Windows-specific) If the stdout is not redirected and the child process is not attached to the current console,
        /// redirected to the null device instead. Note that <see cref="ChildProcessFlags.AttachToCurrentConsole"/> is unset by default.
        /// </para>
        /// </summary>
        ParentOutput,

        /// <summary>
        /// <para>Redirected to the stderr of the current process.</para>
        /// <para>
        /// (Windows-specific) If the stderr is not redirected and the child process is not attached to the current console,
        /// redirected to the null device instead. Note that <see cref="ChildProcessFlags.AttachToCurrentConsole"/> is unset by default.
        /// </para>
        /// </summary>
        ParentError,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the <see cref="IChildProcess.StandardOutput"/> property.
        /// </summary>
        OutputPipe,

        /// <summary>
        /// Redirected to a newly created pipe. The counterpart of that pipe will be exposed as the <see cref="IChildProcess.StandardError"/> property.
        /// </summary>
        ErrorPipe,

        /// <summary>
        /// Redirected to a file. The existing content of the file will be truncated. The corresponding <see cref="ChildProcessStartInfo.StdOutputFile"/>
        /// or <see cref="ChildProcessStartInfo.StdErrorFile"/> property must also be set.
        /// </summary>
        File,

        /// <summary>
        /// Redirected to a file. New bytes written will be appended to the file. The corresponding <see cref="ChildProcessStartInfo.StdOutputFile"/>
        /// or <see cref="ChildProcessStartInfo.StdErrorFile"/> property must also be set.
        /// </summary>
        AppendToFile,

        /// <summary>
        /// Redirected to a file handle. The corresponding <see cref="ChildProcessStartInfo.StdOutputHandle"/>
        /// or <see cref="ChildProcessStartInfo.StdErrorHandle"/> property must also be set.
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
        /// The <see cref="FileName"/> and <see cref="Arguments"/> properties must be set later.
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
        /// (Windows-specific) If <see cref="ChildProcessFlags.UseCustomCodePage"/> is set,
        /// specifies the code page that should be used by newly created consoles (if any).
        /// The default value is 65001 (UTF-8).
        /// </summary>
        public int CodePage { get; set; } = 65001;

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
