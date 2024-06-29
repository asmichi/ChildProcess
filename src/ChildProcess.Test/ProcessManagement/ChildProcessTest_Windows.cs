// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

            sut.StandardInput.Close();
            sut.WaitForExit();
        }

        // NOTE: EnableHandle is tested by ChildProcessTest_Handle because in the future it can be supported on Linux versions with pidfd support.
        [Theory]
        [InlineData(ChildProcessFlags.DisableArgumentQuoting)]
        [InlineData(ChildProcessFlags.CreateSuspended)]
        [InlineData(ChildProcessFlags.DisableKillOnDispose)]
        public void RejectsWindowsSpecificFlagsOnUnsupportedPlatforms(ChildProcessFlags windowsSpecificFlag)
        {
            // CreateSuspended is Windows-specific.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath)
            {
                Flags = windowsSpecificFlag,
            };

            Assert.Throws<PlatformNotSupportedException>(() => { ChildProcess.Start(si).Dispose(); });
        }

        [Theory]
        [InlineData("foo.bat")]
        [InlineData("foo.BaT")]
        [InlineData("foo.cmd")]
        [InlineData("foo.CmD")]
        public void RejectsBatchFiles(string batchFileName)
        {
            // Testing Windows-specific behavior.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            using var temp = new TemporaryDirectory();

            // Create a batch file.
            var batchFilePath = Path.Join(temp.Location, batchFileName);
            File.WriteAllText(batchFilePath, "exit /b 1", Encoding.ASCII);

            var si = new ChildProcessStartInfo(batchFilePath)
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            Assert.Throws<ChildProcessStartingBlockedException>(() => { ChildProcess.Start(si).Dispose(); });
        }

        [Theory]
        [InlineData("cmd.exe")]
        [InlineData("CmD.ExE")]
        public void CanStartCmdExeIfDisableArgumentQuoting(string cmdExeFileName)
        {
            // Testing Windows-specific behavior.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(cmdExeFileName, "/c", "echo", "foo")
            {
                Flags = ChildProcessFlags.DisableArgumentQuoting,
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            Assert.Equal("foo", ExecuteForStandardOutput(si, Encoding.ASCII).Trim());
        }

        [Theory]
        [InlineData("cmd.exe")]
        [InlineData("CmD.ExE")]
        public void RejectsStartingCmdExeWithoutDisableArgumentQuoting(string cmdExeFileName)
        {
            // Testing Windows-specific behavior.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(cmdExeFileName, "/c", "echo", "foo")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            Assert.Throws<ChildProcessStartingBlockedException>(() => { ChildProcess.Start(si).Dispose(); });
        }

        [Fact]
        public void TestKillOnDispose()
        {
            // Code pages are Windows-specific.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "ExitCode", "0")
            {
                Flags = ChildProcessFlags.CreateSuspended | ChildProcessFlags.EnableHandle,
            };

            using var p = ChildProcess.Start(si);

            // Disposing p will close the process handle, so create our own duplicate.
            using var waitHandle = new WindowsProcessWaitHandle(p.Handle);

            p.Dispose();

            Assert.True(waitHandle.WaitOne(100));
        }
    }
}
