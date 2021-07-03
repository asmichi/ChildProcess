// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Asmichi.PlatformAbstraction;
using Xunit;
using KV = System.Collections.Generic.KeyValuePair<string, string>;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessCreationContextTest
    {
        public static readonly object[][] ConstructionTestCases = new object[][]
        {
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("B", "B"), new("A", "A") },
            },
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B") },
                new KV[] { new("A", "A"), new("B", "B") },
            },
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C") },
                new KV[] { new("A", "A"), new("C", "C"), new("B", "B") },
            },
        };

        [Theory]
        [MemberData(nameof(ConstructionTestCases))]
        public void Construction(KV[] expected, KV[] input)
        {
            var sut = new ChildProcessCreationContext(input);
            Assert.Equal(expected, sut.EnvironmentVariables);
        }

        public static readonly object[][] ConstructionCaseSensitiveTestCases = new object[][]
        {
            // preserves names that differ only in cases
            new object[2]
            {
                new KV[] { new("A", "A"), new("a", "a") },
                new KV[] { new("A", "A"), new("a", "a") },
            },
        };

        [Theory]
        [MemberData(nameof(ConstructionCaseSensitiveTestCases))]
        public void ConstructionCaseSensitive(KV[] expected, KV[] input)
        {
            if (!EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            var sut = new ChildProcessCreationContext(input);
            Assert.Equal(expected, sut.EnvironmentVariables);
        }

        [Fact]
        public void AcceptsEmptyValue()
        {
            var input = new KV[] { new("A", "") };
            var sut = new ChildProcessCreationContext(input);
            Assert.Equal(input, sut.EnvironmentVariables);
        }

        [Fact]
        public void RejectsNullValue()
        {
            var input = new KV[] { new("A", null!) };
            Assert.Throws<ArgumentException>(() => new ChildProcessCreationContext(input));
        }

        public static readonly object[][] RejectsDuplicatesTestCases = new object[][]
        {
            new object[1] { new KV[] { new("A", "A"), new("B", "B"), new("A", "A") } },
        };

        [Theory]
        [MemberData(nameof(RejectsDuplicatesTestCases))]
        public void RejectsDuplicates(KV[] input)
        {
            Assert.Throws<ArgumentException>(() => new ChildProcessCreationContext(input));
        }

        public static readonly object[][] RejectsDuplicatesCaseInsensitiveTestCases = new object[][]
        {
            // basic
            new object[1] { new KV[] { new("A", "A"), new("B", "B"), new("a", "a") } },
        };

        [Theory]
        [MemberData(nameof(RejectsDuplicatesCaseInsensitiveTestCases))]
        public void RejectsDuplicatesCaseInsensitive(KV[] input)
        {
            if (EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            Assert.Throws<ArgumentException>(() => new ChildProcessCreationContext(input));
        }
    }
}
