// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Asmichi.PlatformAbstraction;
using static Asmichi.ProcessManagement.EnvironmentVariableListCreation;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Holds internal creation parameters in addition to <see cref="ChildProcessStartInfo"/>.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct ChildProcessStartInfoInternal
    {
        public readonly string? FileName;
        public readonly IReadOnlyCollection<string> Arguments;
        public readonly string? WorkingDirectory;
        public readonly ChildProcessFlags Flags;
        public readonly int CodePage;
        public readonly IReadOnlyList<string>? SearchPath;
        public readonly InputRedirection StdInputRedirection;
        public readonly OutputRedirection StdOutputRedirection;
        public readonly OutputRedirection StdErrorRedirection;
        public readonly string? StdInputFile;
        public readonly string? StdOutputFile;
        public readonly string? StdErrorFile;
        public readonly SafeHandle? StdInputHandle;
        public readonly SafeHandle? StdOutputHandle;
        public readonly SafeHandle? StdErrorHandle;

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
        public bool DisableWindowsErrorReportingDialog => !Flags.HasEnableWindowsErrorReportingDialog();
    }
}
