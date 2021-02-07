// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Asmichi.Utilities.PlatformAbstraction;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Holds internal creation parameters in addition to <see cref="ChildProcessStartInfo"/>.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct ChildProcessStartInfoInternal
    {
        public string? FileName;
        public IReadOnlyCollection<string> Arguments;
        public string? WorkingDirectory;
        public IReadOnlyCollection<KeyValuePair<string, string>>? EnvironmentVariables;
        public ChildProcessFlags Flags;
        public int CodePage;
        public IReadOnlyList<string>? SearchPath;
        public InputRedirection StdInputRedirection;
        public OutputRedirection StdOutputRedirection;
        public OutputRedirection StdErrorRedirection;
        public string? StdInputFile;
        public string? StdOutputFile;
        public string? StdErrorFile;
        public SafeFileHandle? StdInputHandle;
        public SafeFileHandle? StdOutputHandle;
        public SafeFileHandle? StdErrorHandle;

        /// <summary>
        /// If the current process is not attached to a console, we automatically create a new pseudo console.
        /// </summary>
        public bool CreateNewConsole;

        public ChildProcessStartInfoInternal(ChildProcessStartInfo startInfo)
        {
            FileName = startInfo.FileName;
            Arguments = startInfo.Arguments;
            WorkingDirectory = startInfo.WorkingDirectory;
            EnvironmentVariables = startInfo.EnvironmentVariables;
            Flags = startInfo.Flags;
            CodePage = startInfo.CodePage;
            SearchPath = startInfo.SearchPath;
            StdInputRedirection = startInfo.StdInputRedirection;
            StdOutputRedirection = startInfo.StdOutputRedirection;
            StdErrorRedirection = startInfo.StdErrorRedirection;
            StdInputFile = startInfo.StdInputFile;
            StdOutputFile = startInfo.StdOutputFile;
            StdErrorFile = startInfo.StdErrorFile;
            StdInputHandle = startInfo.StdInputHandle;
            StdOutputHandle = startInfo.StdOutputHandle;
            StdErrorHandle = startInfo.StdErrorHandle;

            // Additional parameters
            CreateNewConsole = !Flags.HasAttachToCurrentConsole() || !ConsolePal.HasConsoleWindow();
        }

        public bool CanSignal => !Flags.HasAttachToCurrentConsole();
    }
}
