// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Asmichi.PlatformAbstraction;
using Microsoft.Win32.SafeHandles;
using static Asmichi.ProcessManagement.EnvironmentVariableListCreation;

namespace Asmichi.ProcessManagement
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
        /// Indicates whether <see cref="EnvironmentVariables"/> should be used.
        /// <see langword="false"/> indicates that the child process inherits the environment variables of the current process as is.
        /// </summary>
        public bool UseCustomEnvironmentVariables;

        /// <summary>
        /// If <see cref="UseCustomEnvironmentVariables"/> is <see langword="true"/>, specifies the environment variables of the child process.
        /// </summary>
        public ReadOnlyMemory<KeyValuePair<string, string>> EnvironmentVariables;

        /// <summary>
        /// <para>Indicates whether a new pseudo console or a process group should be created.</para>
        /// <para>(Windows-specific) If the current process is not attached to a console, we automatically create a new pseudo console.</para>
        /// </summary>
        public bool CreateNewConsole;

        public ChildProcessStartInfoInternal(ChildProcessStartInfo startInfo)
        {
            var flags = startInfo.Flags;

            FileName = startInfo.FileName;
            Arguments = startInfo.Arguments;
            WorkingDirectory = startInfo.WorkingDirectory;
            Flags = flags;
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

            if (!flags.HasDisableEnvironmentVariableInheritance()
                && startInfo.CreationContext is null
                && startInfo.ExtraEnvironmentVariables.Count == 0)
            {
                UseCustomEnvironmentVariables = false;
                EnvironmentVariables = default;
            }
            else
            {
                UseCustomEnvironmentVariables = true;

                if (flags.HasDisableEnvironmentVariableInheritance())
                {
                    EnvironmentVariables = SortExtraEnvVars(startInfo.ExtraEnvironmentVariables);
                }
                else if (startInfo.CreationContext is null)
                {
                    EnvironmentVariables = MergeExtraEnvVarsWithProcess(startInfo.ExtraEnvironmentVariables);
                }
                else
                {
                    EnvironmentVariables = MergeExtraEnvVarsWithContext(startInfo.CreationContext.EnvironmentVariablesInternal, startInfo.ExtraEnvironmentVariables);
                }
            }

            // Additional parameters
            CreateNewConsole = !Flags.HasAttachToCurrentConsole() || !ConsolePal.HasConsoleWindow();
        }

        public bool AllowSignal => !Flags.HasAttachToCurrentConsole();
    }
}
