// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Asmichi.Utilities.ProcessManagement
{
    public sealed class ChildProcessTest_Redirection
    {
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
            using var srOut = new StreamReader(sut.StandardOutput);
            using var srErr = new StreamReader(sut.StandardError);
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
            Assert.True(IsAsync(sut.StandardInput));
            Assert.True(IsAsync(sut.StandardOutput));
            Assert.True(IsAsync(sut.StandardError));

            using (var sr = new StreamReader(sut.StandardOutput))
            {
                const string text = "foobar";
                var stdoutTask = sr.ReadToEndAsync();
                using (var sw = new StreamWriter(sut.StandardInput))
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
    }
}
