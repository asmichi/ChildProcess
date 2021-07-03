// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Asmichi.ProcessManagement;

namespace Asmichi
{
    public static class ChildProcessExamplesWindows
    {
        public static async Task Run()
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

            var si = new ChildProcessStartInfo("cmd", "/C", "set")
            {
                ExtraEnvironmentVariables = new Dictionary<string, string> { { "A", "A" } },
                StdOutputRedirection = OutputRedirection.File,
                StdErrorRedirection = OutputRedirection.File,
                StdOutputFile = tempFile,
                StdErrorFile = tempFile,
                Flags = ChildProcessFlags.UseCustomCodePage,
                CodePage = Encoding.Default.CodePage, // UTF-8 on .NET Core
            };

            using (var p = ChildProcess.Start(si))
            {
                await p.WaitForExitAsync();
            }

            // A=A
            // ALLUSERSPROFILE=C:\ProgramData
            // ...
            Console.WriteLine(File.ReadAllText(tempFile));
            File.Delete(tempFile);
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
                tasks[i] = SpawnCmdAsync(i);
            }

            // Spawned 128 processes.
            // ERROR: Timed out waiting for 'pause5'.
            // (snip)
            // ERROR: Timed out waiting for 'pause127'.
            // The 128 processes have exited.
            // Elapsed Time: 3262 ms
            Console.WriteLine("Spawned {0} processes.", N);
            await Task.WhenAll(tasks);
            Console.WriteLine("The {0} processes have exited.", N);
            Console.WriteLine("Elapsed Time: {0} ms", stopWatch.ElapsedMilliseconds);

            static async Task SpawnCmdAsync(int i)
            {
                var si = new ChildProcessStartInfo("waitfor", "/T", "3", $"pause{i}")
                {
                    StdInputRedirection = InputRedirection.ParentInput,
                    StdOutputRedirection = OutputRedirection.NullDevice,
                    Flags = ChildProcessFlags.AttachToCurrentConsole,
                };

                using var p = ChildProcess.Start(si);
                await p.WaitForExitAsync();
            }
        }
    }
}
