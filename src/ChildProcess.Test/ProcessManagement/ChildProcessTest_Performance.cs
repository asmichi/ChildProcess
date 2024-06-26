// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Asmichi.Utilities;
using Xunit;

namespace Asmichi.ProcessManagement
{
    public class ChildProcessTest_Performance
    {
        // TODO: This test is flaky. This cannot be done as part of unit tests and should be an independent benchmark done in a separate process.
        [Fact(Skip = "Super flaky on Azure macOS image")]
        public void ChildProcessWaitForAsyncIsTrulyAsynchronous()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "Sleep", "1000")
            {
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);
            WaitForAsyncIsTrulyAsynchronous(sut);
            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);
        }

        private static void WaitForAsyncIsTrulyAsynchronous(IChildProcess sut)
        {
            var sw = Stopwatch.StartNew();
            // Because WaitForExitAsync is truly asynchronous and does not block a thread-pool thread,
            // we can create WaitForExitAsync tasks without consuming thread-pool threads.
            // In other words, if WaitForExitAsync would consume a thread-pool thread, the works queued by Task.Run would be blocked.
            var waitTasks =
                Enumerable.Range(0, Environment.ProcessorCount * 8)
                .Select(_ => sut.WaitForExitAsync(1000))
                .ToArray();
            Assert.True(waitTasks.All(x => !x.IsCompleted));

            var emptyTasks =
                Enumerable.Range(0, Environment.ProcessorCount * 8)
                .Select(_ => Task.Run(() => { }))
                .ToArray();
            Task.WaitAll(emptyTasks);

            Assert.True(sw.ElapsedMilliseconds < 100);
        }
    }
}
