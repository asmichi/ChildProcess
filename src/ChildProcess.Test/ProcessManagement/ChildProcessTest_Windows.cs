// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Asmichi.Utilities.ProcessManagement
{
    public sealed class ChildProcessTest_Windows
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

                using var sut = ChildProcess.Start(si);
                using var sr = new StreamReader(sut.StandardOutput);
                var output = sr.ReadToEnd();
                sut.WaitForExit();

                Assert.Equal(0, sut.ExitCode);
                Assert.Equal(codePage.ToString(CultureInfo.InvariantCulture), output);
            }
        }
    }
}
