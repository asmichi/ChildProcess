// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Asmichi.Utilities.Utilities;
using Xunit;

namespace Asmichi.Utilities.ProcessManagement
{
    public sealed class ChildProcessTest_Creation
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
            using var sr = new StreamReader(sut.StandardOutput);
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
        public void CanSetWorkingDirectory()
        {
            using var tmp = new TemporaryDirectory();
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommandName, TestUtil.TestChildPath, "EchoWorkingDirectory")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                WorkingDirectory = tmp.Location,
            };

            using var sut = ChildProcess.Start(si);
            using var sr = new StreamReader(sut.StandardOutput);
            var output = sr.ReadToEnd();
            sut.WaitForExit();
            Assert.Equal(tmp.Location, output);
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
            using var sr = new StreamReader(sut.StandardOutput);
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
    }
}
