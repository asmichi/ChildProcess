// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using Asmichi.PlatformAbstraction;
using Xunit;
using KV = System.Collections.Generic.KeyValuePair<string, string>;

namespace Asmichi.Utilities
{
    public sealed class EnvironmentVariableListUtilTest
    {
        public static readonly object[][] TestToSortedKeyValuePairsTestCases = new object[][]
        {
            // basic
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C") },
                new KV[] { new("C", "C"), new("B", "B"), new("A", "A") },
            },
        };

        [Theory]
        [MemberData(nameof(TestToSortedKeyValuePairsTestCases))]
        public void TestToSortedKeyValuePairs(KV[] expected, KV[] input)
        {
            var dictionary = new Dictionary<string, string>(input);
            var actual = EnvironmentVariableListUtil.ToSortedKeyValuePairs(dictionary);

            Assert.Equal(expected, actual);
        }

        public static readonly object[][] TestToSortedKeyValuePairsCaseInsensitiveTestCases = new object[][]
        {
            // OrdinalIgnoreCase then Ordinal
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("b", "b"), new("C", "C") },
                new KV[] { new("C", "C"), new("b", "b"), new("B", "B"), new("A", "A") },
            },
        };

        [Theory]
        [MemberData(nameof(TestToSortedKeyValuePairsCaseInsensitiveTestCases))]
        public void TestToSortedKeyValuePairsCaseInsensitive(KV[] expected, KV[] input)
        {
            if (EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            var dictionary = new Dictionary<string, string>(input);
            var actual = EnvironmentVariableListUtil.ToSortedKeyValuePairs(dictionary);

            Assert.Equal(expected, actual);
        }

        public static readonly object[][] TestToSortedKeyValuePairsCaseSensitiveTestCases = new object[][]
        {
            // Ordinal
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C"), new("b", "b") },
                new KV[] { new("C", "C"), new("b", "b"), new("B", "B"), new("A", "A") },
            },
        };

        [Theory]
        [MemberData(nameof(TestToSortedKeyValuePairsCaseSensitiveTestCases))]
        public void TestToSortedKeyValuePairsCaseSensitive(KV[] expected, KV[] input)
        {
            if (!EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            var dictionary = new Dictionary<string, string>(input);
            var actual = EnvironmentVariableListUtil.ToSortedKeyValuePairs(dictionary);

            Assert.Equal(expected, actual);
        }

        public static readonly object[][] TestToSortedDistinctKeyValuePairsTestCases = new object[][]
        {
            // no removal
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C") },
                new KV[] { new("C", "C"), new("B", "B"), new("A", "A") },
            },
            // smallest one wins
            new object[2]
            {
                new KV[] { new("B", "B") },
                new KV[] { new("b", "b"), new("B", "B") },
            },
            new object[2]
            {
                new KV[] { new("A", "A"), new("B", "B"), new("C", "C") },
                new KV[] { new("C", "C"), new("b", "b"), new("B", "B"), new("A", "A") },
            },
        };

        [Theory]
        [MemberData(nameof(TestToSortedDistinctKeyValuePairsTestCases))]
        public void TestToSortedDistinctKeyValuePairs(KV[] expected, KV[] input)
        {
            if (EnvironmentPal.IsEnvironmentVariableNameCaseSensitive)
            {
                return;
            }

            var dictionary = new Dictionary<string, string>(input);
            var actual = EnvironmentVariableListUtil.ToSortedDistinctKeyValuePairs(dictionary);

            Assert.Equal(expected, actual);
        }
    }
}
