// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Asmichi.ProcessManagement;

namespace Asmichi
{
    public static class ChildProcessExamplesUnix
    {
        public static async Task Run()
        {
            WriteHeader(nameof(BasicAsync));
            await BasicAsync();

            WriteHeader(nameof(RedirectionToFileAsync));
            await RedirectionToFileAsync();

            WriteHeader(nameof(TruePipingAsync));
            await TruePipingAsync();

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
            var si = new ChildProcessStartInfo("sh", "-c", "echo foo")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                // Works like 2>&1
                StdErrorRedirection = OutputRedirection.OutputPipe,
            };

            using var p = ChildProcess.Start(si);
            using (var sr = new StreamReader(p.StandardOutput))
            {
                // "foo"
                Console.Write(await sr.ReadToEndAsync());
            }
            await p.WaitForExitAsync();
            // ExitCode: 0
            Console.WriteLine("ExitCode: {0}", p.ExitCode);
        }

        private static async Task RedirectionToFileAsync()
        {
            var tempFile = Path.GetTempFileName();

            var si = new ChildProcessStartInfo("env")
            {
                ExtraEnvironmentVariables = new Dictionary<string, string> { { "A", "A" } },
                StdOutputRedirection = OutputRedirection.File,
                StdErrorRedirection = OutputRedirection.File,
                StdOutputFile = tempFile,
                StdErrorFile = tempFile,
            };

            using (var p = ChildProcess.Start(si))
            {
                await p.WaitForExitAsync();
            }

            // LANG=C.UTF-8
            // ...
            Console.WriteLine(File.ReadAllText(tempFile));
            File.Delete(tempFile);
        }

        // True piping: you can pipe the output of a child into another child without ever reading the output.
        private static async Task TruePipingAsync()
        {
            // Create an anonymous pipe.
            using var inPipe = new AnonymousPipeServerStream(PipeDirection.In);

            var si1 = new ChildProcessStartInfo("env")
            {
                // Connect the output to writer side of the pipe.
                StdOutputRedirection = OutputRedirection.Handle,
                StdErrorRedirection = OutputRedirection.Handle,
                StdOutputHandle = inPipe.ClientSafePipeHandle,
                StdErrorHandle = inPipe.ClientSafePipeHandle,
            };

            var si2 = new ChildProcessStartInfo("grep", "PATH")
            {
                // Connect the input to the reader side of the pipe.
                StdInputRedirection = InputRedirection.Handle,
                StdInputHandle = inPipe.SafePipeHandle,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.OutputPipe,
            };

            using var p1 = ChildProcess.Start(si1);
            using var p2 = ChildProcess.Start(si2);

            // Close our copy of the pipe handles. (Otherwise p2 will get stuck while reading from the pipe.)
            inPipe.DisposeLocalCopyOfClientHandle();
            inPipe.Close();

            using (var sr = new StreamReader(p2.StandardOutput))
            {
                // PATH=...
                Console.Write(await sr.ReadToEndAsync());
            }

            await p1.WaitForExitAsync();
            await p2.WaitForExitAsync();
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
            Console.WriteLine("Spawned {0} 'sleep 3s' processes.", N);
            await Task.WhenAll(tasks);
            Console.WriteLine("The {0} processes have exited.", N);
            Console.WriteLine("Elapsed Time: {0} ms", stopWatch.ElapsedMilliseconds);

            static async Task SpawnCmdAsync()
            {
                var si = new ChildProcessStartInfo("sleep", "3s");
                using var p = ChildProcess.Start(si);
                await p.WaitForExitAsync();
            }
        }
    }
}
