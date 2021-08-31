// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities;
using Xunit;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessTest_Waiting
    {
        [Fact]
        public void CanObtainExitCode()
        {
            {
                var si = new ChildProcessStartInfo(
                    TestUtil.DotnetCommandName,
                    TestUtil.TestChildPath,
                    "ExitCode",
                    "0");

                using var sut = ChildProcess.Start(si);
                sut.WaitForExit();
                Assert.Equal(0, sut.ExitCode);
            }

            {
                // An exit code is 32-bit on Windows while it is 8-bit on POSIX.
                int nonZeroExitCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? unchecked((int)0xc0000005) : 255;

                var si = new ChildProcessStartInfo(
                    TestUtil.DotnetCommandName,
                    TestUtil.TestChildPath,
                    "ExitCode",
                    nonZeroExitCode.ToString(CultureInfo.InvariantCulture));

                using var sut = ChildProcess.Start(si);
                sut.WaitForExit();
                Assert.NotEqual(0, sut.ExitCode);
                Assert.Equal(nonZeroExitCode, sut.ExitCode);
            }
        }

        [Fact]
        public void ExitCodeThrowsBeforeChildExits()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
            };

            using var sut = ChildProcess.Start(si);
            Assert.Throws<InvalidOperationException>(() => sut.ExitCode);

            sut.StandardInput.Close();
            sut.WaitForExit();

            Assert.Equal(0, sut.ExitCode);
        }

        [Fact]
        public void WaitForExitTimesOut()
        {
            using var sut = CreateForWaitForExitTest();
            Assert.False(sut.WaitForExit(0));
            Assert.False(sut.WaitForExit(1));

            sut.StandardInput.Close();
            sut.ExitedWaitHandle.WaitOne();

            Assert.True(sut.WaitForExit(0));
        }

        [Fact]
        public async Task WaitForExitAsyncTimesOut()
        {
            using var sut = CreateForWaitForExitTest();
            Assert.False(await sut.WaitForExitAsync(0));
            Assert.False(await sut.WaitForExitAsync(1));

            sut.StandardInput.Close();
            sut.ExitedWaitHandle.WaitOne();

            Assert.True(await sut.WaitForExitAsync(0));
        }

        [Fact]
        public async Task CanCancelWaitForExitAsync()
        {
            using var sut = CreateForWaitForExitTest();
            Assert.False(await sut.WaitForExitAsync(0));

            using (var cts = new CancellationTokenSource())
            {
                var t = sut.WaitForExitAsync(1000, cts.Token);
                cts.Cancel();
                Assert.Throws<TaskCanceledException>(() => t.GetAwaiter().GetResult());
            }

            sut.StandardInput.Close();
            sut.ExitedWaitHandle.WaitOne();

            // If the process has already exited, returns true instead of returning CanceledTask.
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.True(await sut.WaitForExitAsync(0, cts.Token));
            }
        }

        private static ChildProcessImpl CreateForWaitForExitTest()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };
            return (ChildProcessImpl)ChildProcess.Start(si);
        }
    }
}
