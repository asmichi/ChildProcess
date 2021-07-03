// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using Asmichi.PlatformAbstraction;
using Xunit;
using KV = System.Collections.Generic.KeyValuePair<string, string>;

namespace Asmichi.Utilities
{
    public sealed class SortedEnvironmentVariableListBuilderTest
    {
        public static readonly object[][] TestNoContextTestCases = new object[][]
        {
            // insert front
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("B", "B"), new("A", "A") },
            },
            // insert last
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B") },
            },
            // insert middle
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C") },
                new KV[] { new("A", "A"), new("C", "C"), new("B", "B") },
            },
            // overwrite
            new object[2]
            {
                new KV[] { new("A", "a"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B"), new("A", "a") },
            },
            // remove front
            new object[2]
            {
                new KV[] { new("B", "B"), new("C", "C") },
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C"), new("A", "") },
            },
            // remove last
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C"), new("C", null!) },
            },
            // remove middle
            new object[2]
            {
                new KV[] { new("A", "A"), new("C", "C") },
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C"), new("B", "") },
            },
        };

        [Theory]
        [MemberData(nameof(TestNoContextTestCases))]
        public void TestNoContext(KV[] expected, KV[] extra)
        {
            var sut = SortedEnvironmentVariableListBuilder.Create(extra.Length);

            foreach (var (name, value) in extra)
            {
                sut.InsertOrRemove(name, value);
            }

            Assert.Equal(expected, sut.Build().ToArray());
        }

        public static readonly object[][] TestWithContextTestCases = new object[][]
        {
            // use context envvars as is
            new object[3]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B") },
                Array.Empty<KV>(),
            },
        };

        [Theory]
        [MemberData(nameof(TestWithContextTestCases))]
        public void TestWithContext(KV[] expected, KV[] context, KV[] extra)
        {
            var sut = SortedEnvironmentVariableListBuilder.CreateFromContext(context.Length + extra.Length, context);

            foreach (var (name, value) in extra)
            {
                sut.InsertOrRemove(name, value);
            }

            Assert.Equal(expected, sut.Build().ToArray());
        }

        public static readonly object[][] TestWithProcessTestCases = new object[][]
        {
            // use context envvars as is
            new object[3]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B") },
                Array.Empty<KV>(),
            },
        };

        [Theory]
        [MemberData(nameof(TestWithProcessTestCases))]
        public void TestWithProcess(KV[] expected, KV[] process, KV[] extra)
        {
            var processDictinary = new Dictionary<string, string>(process);
            var sut = SortedEnvironmentVariableListBuilder.CreateFromProcess(process.Length + extra.Length, processDictinary);

            foreach (var (name, value) in extra)
            {
                sut.InsertOrRemove(name, value);
            }

            Assert.Equal(expected, sut.Build().ToArray());
        }

        public static readonly object[][] TestWithProcessCaseInsensitiveTestCases = new object[][]
        {
            // preserves names that differ only in cases
            new object[3]
            {
                new KV[] { new("A", "A"), new("a", "a") },
                new KV[] { new("A", "A"), new("a", "a") },
                Array.Empty<KV>(),
            },
            // overwrite all
            new object[3]
            {
                new KV[] { new("BB", "bb") },
                new KV[] { new("BB", "BB"), new("Bb", "Bb"), new("bb", "bb") },
                new KV[] { new("BB", "bb") },
            },
            new object[3]
            {
                new KV[] { new("A", "A"), new("BB", "bb"), new("C", "C"), },
                new KV[] { new("A", "A"), new("BB", "BB"), new("Bb", "Bb"), new("bb", "bb"), new("C", "C") },
                new KV[] { new("BB", "bb") },
            },
            // remove all
            new object[3]
            {
                Array.Empty<KV>(),
                new KV[] { new("BB", "BB"), new("Bb", "Bb"), new("bb", "bb") },
                new KV[] { new("BB", "") },
            },
            new object[3]
            {
                new KV[] { new("A", "A"), new("C", "C"), },
                new KV[] { new("A", "A"), new("BB", "BB"), new("Bb", "Bb"), new("bb", "bb"), new("C", "C") },
                new KV[] { new("BB", "") },
            },
        };

        [Theory]
        [MemberData(nameof(TestWithProcessCaseInsensitiveTestCases))]
        public void TestWithProcessCaseInsensitive(KV[] expected, KV[] process, KV[] extra)
        {
            if (EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            var processDictinary = new Dictionary<string, string>(process);
            var sut = SortedEnvironmentVariableListBuilder.CreateFromProcess(process.Length + extra.Length, processDictinary);

            foreach (var (name, value) in extra)
            {
                sut.InsertOrRemove(name, value);
            }

            Assert.Equal(expected, sut.Build().ToArray());
        }
    }
}
