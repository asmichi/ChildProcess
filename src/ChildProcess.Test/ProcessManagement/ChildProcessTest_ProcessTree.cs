// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO.Pipes;
using Asmichi.Utilities;
using Xunit;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessTest_ProcessTree
    {
        // Finite duration so the children will eventually exit, but long enough to allow our tests to wait for the children.
        private const string SleepMilliseconds = "120000";

        [Theory]
        [InlineData(Signal.Interrupt)]
        [InlineData(Signal.Termination)]
        [InlineData(Signal.Kill)]
        public void SignalsProcessTree(Signal signal)
        {
            using var stdOutputPipe = new AnonymousPipeServerStream(PipeDirection.In);
            using var sut = CreateProcessTree(stdOutputPipe);

            switch (signal)
            {
                case Signal.Interrupt:
                    sut.SignalInterrupt();
                    break;

                case Signal.Termination:
                    sut.SignalTermination();
                    break;

                case Signal.Kill:
                    sut.Kill();
                    break;

                default:
                    throw new ArgumentException("should never be reached", nameof(signal));
            }

            sut.WaitForExit();

            // Check that the grand child has been killed.
            // If the grand child is still running, this read will block and eventually read 'E'.
            Assert.Equal(-1, stdOutputPipe.ReadByte());

            Assert.NotEqual(0, sut.ExitCode);
        }

        [Fact]
        public void DisposeTerminatesProcessTree()
        {
            using var stdOutputPipe = new AnonymousPipeServerStream(PipeDirection.In);
            using var sut = CreateProcessTree(stdOutputPipe);

            sut.Dispose();

            // Check that the grand child has been killed.
            // If the grand child is still running, this read will block and eventually read 'E'.
            Assert.Equal(-1, stdOutputPipe.ReadByte());
        }

        private static IChildProcess CreateProcessTree(AnonymousPipeServerStream stdOutputPipe)
        {
            var si = new ChildProcessStartInfo(
                TestUtil.DotnetCommandName,
                TestUtil.TestChildPath,
                "SpawnAndWait",
                TestUtil.DotnetCommandName,
                TestUtil.TestChildPath,
                "EchoAndSleepAndEcho",
                "S",
                SleepMilliseconds,
                "Exited")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.Handle,
                StdOutputHandle = stdOutputPipe.ClientSafePipeHandle,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            var p = ChildProcess.Start(si);

            try
            {
                stdOutputPipe.DisposeLocalCopyOfClientHandle();

                // Wait for the grand child to echo.
                Assert.Equal((byte)'S', stdOutputPipe.ReadByte());
            }
            catch
            {
                p.Dispose();
                throw;
            }

            return p;
        }

        public enum Signal
        {
            Interrupt,
            Termination,
            Kill,
        }
    }
}
