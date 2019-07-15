// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Asmichi.Utilities.ProcessManagement;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities
{
    public static class ChildProcessExamples
    {
        public static async Task Main()
        {
            WriteHeader(nameof(BasicAsync));
            await BasicAsync();

            WriteHeader(nameof(RedirectionToFileAsync));
            await RedirectionToFileAsync();

            WriteHeader(nameof(WaitForExitAsync));
            await WaitForExitAsync();
        }

        private static void WriteHeader(string name)
        {
            Console.WriteLine();
            Console.WriteLine("*** {0}", name);
        }

        private static async Task BasicAsync()
        {
            var si = new ChildProcessStartInfo("cmd", "/C", "echo", "foo")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using (var p = ChildProcess.Start(si))
            {
                using (var sr = new StreamReader(p.StandardOutput))
                {
                    // "foo"
                    Console.Write(await sr.ReadToEndAsync());
                }
                await p.WaitForExitAsync();
                // ExitCode: 0
                Console.WriteLine("ExitCode: {0}", p.ExitCode);
            }
        }

        private static async Task RedirectionToFileAsync()
        {
            var si = new ChildProcessStartInfo("cmd", "/C", "set")
            {
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = "env.txt",
            };

            using (var p = ChildProcess.Start(si))
            {
                await p.WaitForExitAsync();
            }

            // ALLUSERSPROFILE=C:\ProgramData
            // ...
            Console.WriteLine(File.ReadAllText("env.txt"));
        }

        // Truely asynchronous WaitForExitAsync: WaitForExitAsync does not consume a thread-pool thread.
        // You will not need a dedicated thread for handling a child process.
        // You can handle more processes than the number of threads.
        private static async Task WaitForExitAsync()
        {
            const int N = 128;

            var stopWatch = Stopwatch.StartNew();
            var tasks = new Task[N];

            for (int i = 0; i < N; i++)
            {
                tasks[i] = SpawnCmdAsync();
            }

            // Spawned 128 processes.
            // The 128 processes have exited.
            // Elapsed Time: 3367 ms
            Console.WriteLine("Spawned {0} processes.", N);
            await Task.WhenAll(tasks);
            Console.WriteLine("The {0} processes have exited.", N);
            Console.WriteLine("Elapsed Time: {0} ms", stopWatch.ElapsedMilliseconds);

            async Task SpawnCmdAsync()
            {
                var si = new ChildProcessStartInfo("cmd", "/C", "timeout", "3")
                {
                    StdInputRedirection = InputRedirection.ParentInput,
                    StdOutputRedirection = OutputRedirection.NullDevice,
                };

                using (var p = ChildProcess.Start(si))
                {
                    await p.WaitForExitAsync();
                }
            }
        }
    }
}
