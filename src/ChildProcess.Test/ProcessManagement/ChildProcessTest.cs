// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities.Utilities;
using Xunit;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities.ProcessManagement
{
    public class ChildProcessTest
    {
        [Fact]
        public void CanCreateChildProcess()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath)
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using var sut = ChildProcess.Start(si);
            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);

            // This closes StandardOutput, which should be acceptable.
            using var sr = new StreamReader(sut.StandardOutput!);
            Assert.Equal("TestChild", sr.ReadToEnd());
        }

        [Fact]
        public void RespectsSearchPath()
        {
            var environmentSearchPath = SearchPathSearcher.ResolveSearchPath(Environment.GetEnvironmentVariable("PATH"));
            var dotnetPath = SearchPathSearcher.FindExecutable(TestUtil.DotnetCommandName, false, environmentSearchPath);

            Assert.NotNull(dotnetPath);

            var searchPath = new[] { Path.GetDirectoryName(dotnetPath)! };
            {
                var psi = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "ExitCode", "0")
                {
                    Flags = ChildProcessFlags.IgnoreSearchPath,
                    SearchPath = searchPath,
                };

                Assert.Throws<FileNotFoundException>(() => ChildProcess.Start(psi));
            }

            {
                var psi = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "ExitCode", "0")
                {
                    SearchPath = searchPath,
                };

                using var p = ChildProcess.Start(psi);
                p.WaitForExit();
            }
        }

        [Fact]
        public void ReportsFileNotFoundError()
        {
            Assert.Throws<FileNotFoundException>(() => ChildProcess.Start(new ChildProcessStartInfo("nonexistentfile")));
            Assert.Throws<FileNotFoundException>(() => ChildProcess.Start(new ChildProcessStartInfo("/nonexistentfile.exe")));
            Assert.Throws<FileNotFoundException>(() => ChildProcess.Start(new ChildProcessStartInfo("/nonexistentdir/nonexistentfile.exe")));
        }

        [Fact]
        public void ReportsCreationFailure()
        {
            using var temp = new TemporaryDirectory();

            // Create a bad executable.
            var badExecutablePath = Path.Join(temp.Location, "bad_executable.exe");
            File.WriteAllBytes(badExecutablePath, new byte[128]);

            Assert.Throws<Win32Exception>(() => ChildProcess.Start(new ChildProcessStartInfo(badExecutablePath)));
        }

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
                Assert.True(sut.IsSuccessful);
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
                Assert.False(sut.IsSuccessful);
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
            Assert.Throws<InvalidOperationException>(() => sut.IsSuccessful);
            Assert.Throws<InvalidOperationException>(() => sut.ExitCode);

            sut.StandardInput!.Close();
            sut.WaitForExit();

            Assert.True(sut.IsSuccessful);
            Assert.Equal(0, sut.ExitCode);
        }

        [Fact]
        public void WaitForExitTimesOut()
        {
            using var sut = CreateForWaitForExitTest();
            Assert.False(sut.WaitForExit(0));
            Assert.False(sut.WaitForExit(1));

            sut.StandardInput!.Close();
            sut.ExitedWaitHandle.WaitOne();

            Assert.True(sut.WaitForExit(0));
        }

        [Fact]
        public async Task WaitForExitAsyncTimesOut()
        {
            using var sut = CreateForWaitForExitTest();
            Assert.False(await sut.WaitForExitAsync(0));
            Assert.False(await sut.WaitForExitAsync(1));

            sut.StandardInput!.Close();
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

            sut.StandardInput!.Close();
            sut.ExitedWaitHandle.WaitOne();

            // If the process has already exited, returns true instead of returning CanceledTask.
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.True(await sut.WaitForExitAsync(0, cts.Token));
            }
        }

        private static ChildProcess CreateForWaitForExitTest()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };
            return (ChildProcess)ChildProcess.Start(si);
        }

        [Fact]
        public async Task CorrectlyConnectOutputPipes()
        {
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                    StdErrorRedirection = OutputRedirection.ErrorPipe,
                };

                using var sut = ChildProcess.Start(si);
                await CorrectlyConnectsPipesAsync(sut, "TestChild.Out", "TestChild.Error");
            }

            {
                // invert stdout and stderr
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.ErrorPipe,
                    StdErrorRedirection = OutputRedirection.OutputPipe,
                };

                using var sut = ChildProcess.Start(si);
                await CorrectlyConnectsPipesAsync(sut, "TestChild.Error", "TestChild.Out");
            }
        }

        private static async Task CorrectlyConnectsPipesAsync(IChildProcess sut, string expectedStdout, string expectedStderr)
        {
            using var srOut = new StreamReader(sut.StandardOutput!);
            using var srErr = new StreamReader(sut.StandardError!);
            var stdoutTask = srOut.ReadToEndAsync();
            var stderrTask = srErr.ReadToEndAsync();
            sut.WaitForExit();

            Assert.Equal(expectedStdout, await stdoutTask);
            Assert.Equal(expectedStderr, await stderrTask);
        }

        [Fact]
        public async Task PipesAreAsynchronous()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.ErrorPipe,
            };

            using var sut = ChildProcess.Start(si);
            Assert.True(IsAsync(sut.StandardInput!));
            Assert.True(IsAsync(sut.StandardOutput!));
            Assert.True(IsAsync(sut.StandardError!));

            using (var sr = new StreamReader(sut.StandardOutput!))
            {
                const string text = "foobar";
                var stdoutTask = sr.ReadToEndAsync();
                using (var sw = new StreamWriter(sut.StandardInput!))
                {
                    await sw.WriteAsync(text);
                }
                Assert.Equal(text, await stdoutTask);
            }

            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);

            static bool IsAsync(Stream stream)
            {
                return stream switch
                {
                    FileStream fs => fs.IsAsync,
                    NetworkStream _ => true, // Trust the runtime; it must be truly async!
                    _ => throw new InvalidOperationException("Unknown stream type."),
                };
            }
        }

        [Fact]
        public void RedirectionToFile()
        {
            using var tmp = new TemporaryDirectory();
            var inFile = Path.Combine(tmp.Location, "in");
            var outFile = Path.Combine(tmp.Location, "out");
            var errFile = Path.Combine(tmp.Location, "err");

            // StdOutputFile StdErrorFile
            {
                // File
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.File,
                    StdOutputFile = outFile,
                    StdErrorRedirection = OutputRedirection.File,
                    StdErrorFile = errFile,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal("TestChild.Out", File.ReadAllText(outFile));
                Assert.Equal("TestChild.Error", File.ReadAllText(errFile));

                // AppendToFile
                si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.AppendToFile,
                    StdOutputFile = errFile,
                    StdErrorRedirection = OutputRedirection.AppendToFile,
                    StdErrorFile = outFile,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal("TestChild.OutTestChild.Error", File.ReadAllText(outFile));
                Assert.Equal("TestChild.ErrorTestChild.Out", File.ReadAllText(errFile));
            }

            // StdInputFile
            {
                const string text = "foobar";
                File.WriteAllText(inFile, text);

                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
                {
                    StdInputRedirection = InputRedirection.File,
                    StdInputFile = inFile,
                    StdOutputRedirection = OutputRedirection.File,
                    StdOutputFile = outFile,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal(text, File.ReadAllText(outFile));
            }
        }

        [Fact]
        public void CanRedirectToSameFile()
        {
            using var tmp = new TemporaryDirectory();
            var outFile = Path.Combine(tmp.Location, "out");

            // StdOutputFile StdErrorFile
            {
                // File
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.File,
                    StdOutputFile = outFile,
                    StdErrorRedirection = OutputRedirection.File,
                    StdErrorFile = outFile,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal("TestChild.OutTestChild.Error", File.ReadAllText(outFile));

                // AppendToFile
                si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.AppendToFile,
                    StdOutputFile = outFile,
                    StdErrorRedirection = OutputRedirection.AppendToFile,
                    StdErrorFile = outFile,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal("TestChild.OutTestChild.ErrorTestChild.OutTestChild.Error", File.ReadAllText(outFile));
            }
        }

        [Fact]
        public void RedirectionToHandle()
        {
            using var tmp = new TemporaryDirectory();
            var inFile = Path.Combine(tmp.Location, "in");
            var outFile = Path.Combine(tmp.Location, "out");
            var errFile = Path.Combine(tmp.Location, "err");

            // StdOutputHandle StdErrorHandle
            {
                using (var fsOut = File.Create(outFile))
                using (var fsErr = File.Create(errFile))
                {
                    // File
                    var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                    {
                        StdOutputRedirection = OutputRedirection.Handle,
                        StdOutputHandle = fsOut.SafeFileHandle,
                        StdErrorRedirection = OutputRedirection.Handle,
                        StdErrorHandle = fsErr.SafeFileHandle,
                    };

                    using var sut = ChildProcess.Start(si);
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal("TestChild.Out", File.ReadAllText(outFile));
                Assert.Equal("TestChild.Error", File.ReadAllText(errFile));
            }

            // StdInputHandle
            {
                const string text = "foobar";
                File.WriteAllText(inFile, text);

                using (var fsIn = File.OpenRead(inFile))
                {
                    var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
                    {
                        StdInputRedirection = InputRedirection.Handle,
                        StdInputHandle = fsIn.SafeFileHandle,
                        StdOutputRedirection = OutputRedirection.File,
                        StdOutputFile = outFile,
                    };

                    using var sut = ChildProcess.Start(si);
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                }

                Assert.Equal(text, File.ReadAllText(outFile));
            }
        }

        [Fact]
        public void CanSetEnvironmentVariables()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "DumpEnvironmentVariables")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                EnvironmentVariables = GetTestEnvironmentVariables(),
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardOutput!);
            var output = sr.ReadToEnd();
            var childEvars = output.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            sut.WaitForExit();

            Assert.Equal(0, sut.ExitCode);
            Assert.Contains("A=a", childEvars);
            Assert.Contains("BB=bb", childEvars);

            static List<KeyValuePair<string, string>> GetTestEnvironmentVariables()
            {
                var evars = new List<KeyValuePair<string, string>>();

#pragma warning disable CS8605 // Unboxing a possibly null value.
                foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
#pragma warning restore CS8605 // Unboxing a possibly null value.
                {
                    var key = (string)de.Key;
                    var value = (string)de.Value!;

                    evars.Add(new KeyValuePair<string, string>(key, value));
                }

                evars.Add(new KeyValuePair<string, string>("A", "a"));
                evars.Add(new KeyValuePair<string, string>("BB", "bb"));

                return evars;
            }
        }

        [Fact]
        public void CanSetWorkingDirectory()
        {
            using var tmp = new TemporaryDirectory();
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoWorkingDirectory")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                WorkingDirectory = tmp.Location,
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardOutput!);
            var output = sr.ReadToEnd();
            sut.WaitForExit();
            Assert.Equal(tmp.Location, output);
        }
    }
}
