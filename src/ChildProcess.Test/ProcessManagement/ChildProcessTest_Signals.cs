// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Asmichi.Utilities.Interop.Windows;
using Xunit;

namespace Asmichi.Utilities.ProcessManagement
{
    public sealed class ChildProcessTest_Windows
    {
        [Fact]
        public void CanSendSignal()
        {
            var si = new ChildProcessStartInfo(TestUtil.TestChildNativePath, "ReportSignal")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using var sut = ChildProcess.Start(si);

            Assert.True(sut.CanSignal);

            Assert.Equal('R', sut.StandardOutput.ReadByte());

            sut.SignalInterrupt();
            Assert.Equal('I', sut.StandardOutput.ReadByte());

            sut.SignalInterrupt();
            Assert.Equal('I', sut.StandardOutput.ReadByte());

            if (!HasWorkaroundForWindows1809)
            {
                // NOTE: On Windows, a console app cannot cancel CTRL_CLOSE_EVENT (generated when the attached pseudo console is closed).
                //       It will be killed after the 5s-timeout elapses. Once we call SignalTermination, we must treat the app as already terminated.
                //       https://docs.microsoft.com/en-us/windows/console/handlerroutine#timeouts
                sut.SignalTermination();
                Assert.Equal('T', sut.StandardOutput.ReadByte());
            }

            sut.Kill();
            sut.WaitForExit();

            Assert.NotEqual(0, sut.ExitCode);
        }

        [Fact]
        public void CannotSendSignalIfAttachedToCurrentConsole()
        {
            var si = new ChildProcessStartInfo(TestUtil.TestChildNativePath, "ReportSignal")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                Flags = ChildProcessFlags.AttachToCurrentConsole,
            };

            using var sut = ChildProcess.Start(si);

            Assert.False(sut.CanSignal);
            Assert.Throws<InvalidOperationException>(() => sut.SignalInterrupt());
            Assert.Throws<InvalidOperationException>(() => sut.SignalTermination());
            Assert.Equal('R', sut.StandardOutput.ReadByte());

            sut.Kill();
            sut.WaitForExit();

            Assert.NotEqual(0, sut.ExitCode);
        }

        private static bool HasWorkaroundForWindows1809 =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && WindowsVersion.NeedsWorkaroundForWindows1809;
    }
}
