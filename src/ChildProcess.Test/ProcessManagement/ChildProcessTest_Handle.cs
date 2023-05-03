// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Asmichi.Interop.Windows;
using Asmichi.Utilities;
using Xunit;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessTest_Handle
    {
        // Currently supported only on Windows.
        private static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [Fact]
        public void CannotObtainHandleByDefault()
        {
            if (!IsSupported)
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath);
            using var sut = ChildProcess.Start(si);

            Assert.False(sut.HasHandle);
            Assert.Throws<NotSupportedException>(() => sut.Handle);
        }

        [Fact]
        public void CanObtainHandle()
        {
            if (!IsSupported)
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                Flags = ChildProcessFlags.EnableHandle,
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);

            Assert.True(sut.HasHandle);
            _ = sut.Handle;
            _ = sut.PrimaryThreadHandle;

            Assert.False(sut.WaitForExit(0));
            Kernel32.TerminateProcess(sut.Handle, -1);
            sut.WaitForExit();

            Assert.Equal(-1, sut.ExitCode);
        }

        [Fact]
        public void RejectsEnableHandleOnUnsupportedPlatforms()
        {
            if (IsSupported)
            {
                return;
            }

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath)
            {
                Flags = ChildProcessFlags.EnableHandle,
            };

            Assert.Throws<PlatformNotSupportedException>(() => { ChildProcess.Start(si).Dispose(); });
        }
    }
}
