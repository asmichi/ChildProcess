// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;
using static Asmichi.Utilities.ProcessManagement.ChildProcessExecutionTestUtil;

namespace Asmichi.Utilities.ProcessManagement
{
    public sealed class ChildProcessTest_Signals
    {
        [Fact]
        public void CanChangeCodePage()
        {
            // Code pages are Windows-specific.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            const int Latin1CodePage = 1252;
            const int Utf8CodePage = 65001;

            AssertOne(Latin1CodePage);
            AssertOne(Utf8CodePage);

            static void AssertOne(int codePage)
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoCodePage")
                {
                    CodePage = codePage,
                    Flags = ChildProcessFlags.UseCustomCodePage,
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                };

                var output = ExecuteForStandardOutput(si);
                Assert.Equal(codePage.ToString(CultureInfo.InvariantCulture), output);
            }
        }

        [Fact]
        public void ThrowsOnChcpFailure()
        {
            // Code pages are Windows-specific.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoCodePage")
            {
                CodePage = 0,
                Flags = ChildProcessFlags.UseCustomCodePage,
            };

            Assert.Throws<ArgumentException>(() => ChildProcess.Start(si));
        }
    }
}
