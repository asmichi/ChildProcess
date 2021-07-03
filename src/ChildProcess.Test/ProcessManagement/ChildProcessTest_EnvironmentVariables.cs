// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Asmichi.Utilities;
using Xunit;
using static Asmichi.ProcessManagement.ChildProcessExecutionTestUtil;
using KV = System.Collections.Generic.KeyValuePair<string, string>;

namespace Asmichi.ProcessManagement
{
    // NOTE: These tests will fail if the current process has "A" or "BB" as environment variables.
    public sealed class ChildProcessTest_EnvironmentVariables
    {
        // Assumes we do not change the environment variables of the current process.
        [Fact]
        public void InheritsEnvironmentVariables()
        {
            var expected = GetProcessEnvVars();

            AssertEnvironmentVariables(expected, null, Array.Empty<KV>(), true);
        }

        [Fact]
        public void CanAddEnvironmentVariables()
        {
            var extraEnvVars = new KV[]
            {
                new("A", "A"),
                new("BB", "BB"),
            };

            var expected = GetProcessEnvVars().Concat(extraEnvVars);

            AssertEnvironmentVariables(expected, null, extraEnvVars, true);
        }

        [Fact]
        public void CanRemoveEnvironmentVariables()
        {
            var extraEnvVars = new KV[]
            {
                new("A", null!),
                new("BB", ""),
            };

            var processEnvVars = GetProcessEnvVars();
            var contextEnvVars = new Dictionary<string, string>(processEnvVars)
            {
                { "A", "A" },
                { "BB", "BB" },
            };
            var context = new ChildProcessCreationContext(contextEnvVars);

            AssertEnvironmentVariables(processEnvVars, context, extraEnvVars, true);
        }

        [Fact]
        public void CanDisableEnvironmentVariableInheritance()
        {
            var nonEmptyProcessEnvVars = GetProcessEnvVars().Where(x => !string.IsNullOrEmpty(x.Value)).ToArray();
            var contextEnvVars = new Dictionary<string, string>(nonEmptyProcessEnvVars)
            {
                { "A", "A" },
                { "BB", "BB" },
            };
            var context = new ChildProcessCreationContext(contextEnvVars);

            AssertEnvironmentVariables(nonEmptyProcessEnvVars, context, nonEmptyProcessEnvVars.ToArray(), false);
        }

        private static void AssertEnvironmentVariables(
            IEnumerable<KV> expected,
            ChildProcessCreationContext? context,
            KV[] extraEnvVars,
            bool inheritFromContext)
        {
            var actual = ExecuteForEnvironmentVariables(context, extraEnvVars, inheritFromContext);
            Assert.Equal(expected.OrderBy(x => x, EnvironmentVariablePairNameComparer.DefaultThenOrdinal), actual);
        }

        private static KV[] ExecuteForEnvironmentVariables(
            ChildProcessCreationContext? context,
            KV[] extraEnvVars,
            bool inheritFromContext)
        {
            var si = new ChildProcessStartInfo(TestUtil.TestChildNativePath, "DumpEnvironmentVariables")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                Flags = inheritFromContext ? ChildProcessFlags.None : ChildProcessFlags.DisableEnvironmentVariableInheritance,
                CreationContext = context,
                ExtraEnvironmentVariables = extraEnvVars,
            };

            var output = ExecuteForStandardOutput(si);
            var childEnvVars =
                output.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ToKeyValuePair)
                .ToArray();
            return childEnvVars;

            static KV ToKeyValuePair(string envVar)
            {
                int index = envVar.IndexOf('=', StringComparison.Ordinal);
                var name = envVar.Substring(0, index);
                var value = envVar.Substring(index + 1);
                return new(name, value);
            }
        }

        private static ArraySegment<KV> GetProcessEnvVars() => EnvironmentVariableListUtil.ToSortedDistinctKeyValuePairs(Environment.GetEnvironmentVariables());
    }
}
