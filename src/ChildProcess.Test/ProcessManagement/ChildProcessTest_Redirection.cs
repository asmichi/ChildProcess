// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Asmichi.Utilities;
using Xunit;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessTest_Redirection
    {
        [Fact]
        public void RejectsInvalidRedirection()
        {
            using var tmp = new TemporaryDirectory();
            var filePath = Path.Combine(tmp.Location, "file");

            // null
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdInputRedirection = InputRedirection.Handle }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdInputRedirection = InputRedirection.File }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdOutputRedirection = OutputRedirection.Handle }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdOutputRedirection = OutputRedirection.File }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdOutputRedirection = OutputRedirection.AppendToFile }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdErrorRedirection = OutputRedirection.Handle }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdErrorRedirection = OutputRedirection.File }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdErrorRedirection = OutputRedirection.AppendToFile }));

            // Enum value out of range
            Assert.Throws<ArgumentOutOfRangeException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdInputRedirection = (InputRedirection)(-1) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdOutputRedirection = (OutputRedirection)(-1) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath) { StdErrorRedirection = (OutputRedirection)(-1) }));

            // When both stdout and stderr are redirected to the same file, they must have the same "append" option.
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath)
                {
                    StdOutputFile = filePath,
                    StdOutputRedirection = OutputRedirection.File,
                    StdErrorFile = filePath,
                    StdErrorRedirection = OutputRedirection.AppendToFile,
                }));
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath)
                {
                    StdOutputFile = filePath,
                    StdOutputRedirection = OutputRedirection.AppendToFile,
                    StdErrorFile = filePath,
                    StdErrorRedirection = OutputRedirection.File,
                }));
        }

        [Fact]
        public void StreamPropertiesThrowWhenNoPipeAssociated()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "ExitCode", "0")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);

            Assert.False(sut.HasStandardInput);
            Assert.False(sut.HasStandardOutput);
            Assert.False(sut.HasStandardError);
            Assert.Throws<InvalidOperationException>(() => sut.StandardInput);
            Assert.Throws<InvalidOperationException>(() => sut.StandardOutput);
            Assert.Throws<InvalidOperationException>(() => sut.StandardError);

            sut.WaitForExit();
        }

        [Fact]
        public void RedirectionToNull()
        {
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
                {
                    StdInputRedirection = InputRedirection.NullDevice,
                    StdOutputRedirection = OutputRedirection.NullDevice,
                    StdErrorRedirection = OutputRedirection.NullDevice,
                };

                using var sut = ChildProcess.Start(si);
                sut.WaitForExit();
                Assert.Equal(0, sut.ExitCode);
            }

            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdInputRedirection = InputRedirection.NullDevice,
                    StdOutputRedirection = OutputRedirection.NullDevice,
                    StdErrorRedirection = OutputRedirection.NullDevice,
                };

                using var sut = ChildProcess.Start(si);
                sut.WaitForExit();
                Assert.Equal(0, sut.ExitCode);
            }
        }

        [Fact]
        public void ConnectsInputPipe()
        {
            using var tmp = new TemporaryDirectory();
            var outFile = Path.Combine(tmp.Location, "out");

            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = outFile,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);
            const string Text = "foo";
            using (var sw = new StreamWriter(sut.StandardInput))
            {
                sw.Write(Text);
            }
            sut.WaitForExit();

            Assert.True(sut.HasStandardInput);
            Assert.False(sut.HasStandardOutput);
            Assert.False(sut.HasStandardError);
            Assert.Equal(Text, File.ReadAllText(outFile));
        }

        [Fact]
        public void ConnectsOutputPipe()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardOutput);
            var output = sr.ReadToEnd();
            sut.WaitForExit();

            Assert.False(sut.HasStandardInput);
            Assert.True(sut.HasStandardOutput);
            Assert.False(sut.HasStandardError);
            Assert.Equal(0, sut.ExitCode);
            Assert.Equal("TestChild.Out", output);
        }

        [Fact]
        public void ConnectsErrorPipe()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdOutputRedirection = OutputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.ErrorPipe,
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardError);
            var output = sr.ReadToEnd();
            sut.WaitForExit();

            Assert.False(sut.HasStandardInput);
            Assert.False(sut.HasStandardOutput);
            Assert.True(sut.HasStandardError);
            Assert.Equal(0, sut.ExitCode);
            Assert.Equal("TestChild.Error", output);
        }

        [Fact]
        public async Task ConnectOutputAndErrorPipes()
        {
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                    StdErrorRedirection = OutputRedirection.ErrorPipe,
                };

                using var sut = ChildProcess.Start(si);
                await Impl(sut, "TestChild.Out", "TestChild.Error");
            }

            {
                // invert stdout and stderr
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.ErrorPipe,
                    StdErrorRedirection = OutputRedirection.OutputPipe,
                };

                using var sut = ChildProcess.Start(si);
                await Impl(sut, "TestChild.Error", "TestChild.Out");
            }

            static async Task Impl(IChildProcess sut, string expectedStdout, string expectedStderr)
            {
                using var srOut = new StreamReader(sut.StandardOutput);
                using var srErr = new StreamReader(sut.StandardError);
                var stdoutTask = srOut.ReadToEndAsync();
                var stderrTask = srErr.ReadToEndAsync();
                sut.WaitForExit();

                Assert.Equal(0, sut.ExitCode);
                Assert.Equal(expectedStdout, await stdoutTask);
                Assert.Equal(expectedStderr, await stderrTask);
            }
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
                const string Text = "foobar";
                var stdoutTask = sr.ReadToEndAsync();
                using (var sw = new StreamWriter(sut.StandardInput))
                {
                    await sw.WriteAsync(Text);
                }
                Assert.Equal(Text, await stdoutTask);
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
                    Assert.Equal(0, sut.ExitCode);
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
                    Assert.Equal(0, sut.ExitCode);
                }

                Assert.Equal("TestChild.OutTestChild.Error", File.ReadAllText(outFile));
                Assert.Equal("TestChild.ErrorTestChild.Out", File.ReadAllText(errFile));
            }

            // StdInputFile
            {
                const string Text = "foobar";
                File.WriteAllText(inFile, Text);

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
                    Assert.Equal(0, sut.ExitCode);
                }

                Assert.Equal(Text, File.ReadAllText(outFile));
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
                    Assert.Equal(0, sut.ExitCode);
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
                    Assert.Equal(0, sut.ExitCode);
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
                    Assert.Equal(0, sut.ExitCode);
                }

                Assert.Equal("TestChild.Out", File.ReadAllText(outFile));
                Assert.Equal("TestChild.Error", File.ReadAllText(errFile));
            }

            // StdInputHandle
            {
                const string Text = "foobar";
                File.WriteAllText(inFile, Text);

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
                    Assert.Equal(0, sut.ExitCode);
                }

                Assert.Equal(Text, File.ReadAllText(outFile));
            }
        }
    }
}
