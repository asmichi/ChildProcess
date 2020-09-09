// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit;
using KV = System.Collections.Generic.KeyValuePair<string, string>;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    public class EnvironmentBlockUtilTest
    {
        public static readonly object[][] TestMakeEnvironmentBlockWin32TestCases = new object[][]
        {
            new object[2] { "A=a\0\0", new[] { new KV("A", "a") } },
            new object[2] { "A=a\0BB=bb\0\0", new[] { new KV("A", "a"), new KV("BB", "bb") } },
        };

        [Theory]
        [MemberData(nameof(TestMakeEnvironmentBlockWin32TestCases))]
        public void TestMakeEnvironmentBlockWin32(string expected, KV[] input)
        {
            Assert.Equal(
                expected.ToCharArray(),
                EnvironmentBlockUtil.MakeEnvironmentBlockWin32(input));
        }
    }
}
