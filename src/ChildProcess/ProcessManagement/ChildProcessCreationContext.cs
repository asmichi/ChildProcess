// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Asmichi.PlatformAbstraction;
using Asmichi.Utilities;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Represents a context that should be used to create child processes.
    /// </summary>
    public sealed class ChildProcessCreationContext
    {
        private IReadOnlyList<KeyValuePair<string, string>>? _envVarsAsROL;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessCreationContext"/> class
        /// with the supplied environment variables.
        /// </summary>
        /// <param name="environmentVariables">
        /// The environment variables that should be passed to child processes. No duplicate names allowed.
        /// Names must not contain '\0' or '='.
        /// Values must not contain '\0'. Values must not be <see langword="null"/>.
        /// Entries can be overridden or removed by <see cref="ChildProcessStartInfo.ExtraEnvironmentVariables"/>.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="environmentVariables"/> contains a duplicate name.</exception>
        /// <exception cref="ArgumentException"><paramref name="environmentVariables"/> contains a name that contains '\0' or '='.</exception>
        /// <exception cref="ArgumentException"><paramref name="environmentVariables"/> contains a value that is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="environmentVariables"/> contains a name that contains '\0'.</exception>
        public ChildProcessCreationContext(
            IReadOnlyCollection<KeyValuePair<string, string>> environmentVariables)
            : this(SortAndValidate(environmentVariables))
        {
        }

        private ChildProcessCreationContext(
            ReadOnlyMemory<KeyValuePair<string, string>> immutableSortedDistinctEnvironmentVariables)
        {
            EnvironmentVariablesInternal = immutableSortedDistinctEnvironmentVariables;
        }

        /// <summary>
        /// The list of the environment variables that should be passed to child processes.
        /// Its entries are sorted by name. It does not contain duplicate names.
        /// </summary>
        /// <remarks>
        /// Entries can be overridden or removed by <see cref="ChildProcessStartInfo.ExtraEnvironmentVariables"/>.
        /// </remarks>
        public IReadOnlyList<KeyValuePair<string, string>> EnvironmentVariables => _envVarsAsROL ??= EnvironmentVariablesInternal.Span.ToArray();

        internal ReadOnlyMemory<KeyValuePair<string, string>> EnvironmentVariablesInternal { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildProcessCreationContext"/> class
        /// with the environment variables of the current process.
        /// </summary>
        /// <remarks>
        /// (Windows-specific) Duplicates will be removed.
        /// This method performs <see cref="StringComparison.Ordinal"/> comparisons and picks the smallest one (uppercase one).
        /// Some broken programs (for example, Cygwin) add names that differ only in cases to the environment block.
        /// </remarks>
        /// <returns>Created instance of the <see cref="ChildProcessCreationContext"/> class.</returns>
        public static ChildProcessCreationContext FromProcessEnvironment()
        {
            var processEnvVars = EnvironmentVariableListUtil.ToSortedDistinctKeyValuePairs(Environment.GetEnvironmentVariables());
            return new ChildProcessCreationContext(processEnvVars.AsMemory());
        }

        private static ReadOnlyMemory<KeyValuePair<string, string>> SortAndValidate(
            IReadOnlyCollection<KeyValuePair<string, string>> environmentVariables)
        {
            var nameComparison = EnvironmentPal.EnvironmentVariableNameComparison;
            var envVars = environmentVariables.ToArray();

            // validate names and values
            foreach (var (name, value) in envVars)
            {
                EnvironmentVariableUtil.ValidateNameAndValue(name, value);

                if (value is null)
                {
                    throw new ArgumentException($"Value of environment variable '{name}' must not be null.", nameof(environmentVariables));
                }
            }

            // sort
            Array.Sort(envVars, EnvironmentVariablePairNameComparer.DefaultThenOrdinal);

            // check dulicates
            for (int i = 1; i < envVars.Length; i++)
            {
                var cur = envVars[i].Key;
                var prev = envVars[i - 1].Key;
                if (string.Equals(cur, prev, nameComparison))
                {
                    throw new ArgumentException($"Duplicate name {prev}.", nameof(environmentVariables));
                }
            }

            return envVars;
        }
    }
}
