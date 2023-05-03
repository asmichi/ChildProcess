// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Asmichi.Interop.Windows;
using Asmichi.Utilities;
using Xunit;
using static Asmichi.ProcessManagement.ChildProcessExecutionTestUtil;

namespace Asmichi.ProcessManagement
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

        [Fact]
        public void CanCreateSuspended()
        {
            // CreateSuspended is Windows-specific.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                Flags = ChildProcessFlags.EnableHandle | ChildProcessFlags.CreateSuspended,
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);

            Assert.True(sut.HasHandle);

            // https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-resumethread
            // The previous suspend count returned by ResumeThread should be 1 if the process has been created suspended.
            Assert.Equal(1, Kernel32.ResumeThread((SafeThreadHandle)sut.PrimaryThreadHandle));
            // Verify the above theory. The suspend count now is 0.
            Assert.Equal(0, Kernel32.ResumeThread((SafeThreadHandle)sut.PrimaryThreadHandle));
        }

        [Fact]
        public void RejectsCreateSuspendedOnUnsupportedPlatforms()
        {
            // CreateSuspended is Windows-specific.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath)
            {
                Flags = ChildProcessFlags.CreateSuspended,
            };

            Assert.Throws<PlatformNotSupportedException>(() => { ChildProcess.Start(si).Dispose(); });
        }
    }
}
