// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Asmichi.Utilities;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Creates an environment variable list passed to a child processes.
    /// </summary>
    internal static class EnvironmentVariableListCreation
    {
        internal static ReadOnlyMemory<KeyValuePair<string, string>> SortExtraEnvVars(
            IReadOnlyCollection<KeyValuePair<string, string>> extraEnvVars)
        {
            if (extraEnvVars.Count == 0)
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            var builder = SortedEnvironmentVariableListBuilder.Create(extraEnvVars.Count);
            builder.InsertOrRemoveRange(extraEnvVars);
            return builder.Build();
        }

        public static ReadOnlyMemory<KeyValuePair<string, string>> MergeExtraEnvVarsWithContext(
            ReadOnlyMemory<KeyValuePair<string, string>> contextEnvVars,
            IReadOnlyCollection<KeyValuePair<string, string>> extraEnvVars)
        {
            if (extraEnvVars.Count == 0)
            {
                // PERF: Use the values from the context as is.
                return contextEnvVars;
            }

            var builder = SortedEnvironmentVariableListBuilder.CreateFromContext(contextEnvVars.Length + extraEnvVars.Count, contextEnvVars);
            builder.InsertOrRemoveRange(extraEnvVars);
            return builder.Build();
        }

        public static ReadOnlyMemory<KeyValuePair<string, string>> MergeExtraEnvVarsWithProcess(
            IReadOnlyCollection<KeyValuePair<string, string>> extraEnvVars)
        {
            Debug.Assert(extraEnvVars.Count != 0, "This case should fall into 'UseCustomEnvironmentVariables = false'.");

            var processEnvVars = Environment.GetEnvironmentVariables();
            var builder = SortedEnvironmentVariableListBuilder.CreateFromProcess(processEnvVars.Count + extraEnvVars.Count, processEnvVars);
            builder.InsertOrRemoveRange(extraEnvVars);
            return builder.Build();
        }
    }
}
