// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.IO;
using Asmichi.Utilities;
using Xunit;
using static Asmichi.ProcessManagement.ChildProcessExecutionTestUtil;

namespace Asmichi.ProcessManagement
{
    public sealed class ChildProcessTest_Creation
    {
        // NOTE: Redirection-related arguments are covered by ChildProcessTest_Redirection.
        [Fact]
        public void RejectsInvalidStartInfos()
        {
            // null
            Assert.Throws<ArgumentException>(
                () => ChildProcess.Start(new ChildProcessStartInfo { FileName = "a", Arguments = null! }));
            Assert.Throws<ArgumentException>(
                () => ChildProcess.Start(new ChildProcessStartInfo { FileName = null, Arguments = Array.Empty<string>() }));

            // Invalid Flags
            Assert.Throws<ArgumentException>(() => ChildProcess.Start(
                new ChildProcessStartInfo(TestUtil.TestChildNativePath)
                { Flags = ChildProcessFlags.AttachToCurrentConsole | ChildProcessFlags.UseCustomCodePage }));
        }

        [Fact]
        public void CanCreateChildProcess()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath)
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardOutput);
            var output = sr.ReadToEnd();

            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);
            Assert.Equal("TestChild", output);
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
        public void CanSetWorkingDirectory()
        {
            using var tmp = new TemporaryDirectory();
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoWorkingDirectory")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                WorkingDirectory = tmp.Location,
            };

            var output = ExecuteForStandardOutput(si);
            Assert.Equal(ToResolvedCurrentDirectory(tmp.Location), output);

            // On macOS, the path to the temporary directory contains symbolic links.
            // Resolve it to a real path since getcwd will return a resolved path (as specified by POSIX).
            static string ToResolvedCurrentDirectory(string path)
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "ToResolvedCurrentDirectory", path)
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                };

                return ExecuteForStandardOutput(si);
            }
        }

        [Fact]
        public void CanObtainProcessId()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };

            using var sut = ChildProcess.Start(si);

            using var p = System.Diagnostics.Process.GetProcessById(sut.Id);
            Assert.StartsWith(Path.GetFileNameWithoutExtension(TestUtil.DotnetCommandName), p.ProcessName, StringComparison.OrdinalIgnoreCase);
            sut.StandardInput.Close();

            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);
        }
    }
}
