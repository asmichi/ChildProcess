// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Asmichi.Utilities
{
    public class SearchPathSearcherTest
    {
        [Fact]
        public void CanFindFile()
        {
            using var temp = new TemporaryDirectory();
            var searchPath = new string[] { temp.Location, Path.Join(temp.Location, "dir") };

            // temp / randomName
            // temp / dir / a
            // temp / dir / randomName
            // temp / dir2 / a (not included in the search path)
            // cwd / randomName
            var randomName = Path.GetRandomFileName();
            Directory.CreateDirectory(Path.Join(temp.Location, "dir"));
            Directory.CreateDirectory(Path.Join(temp.Location, "dir2"));
            CreateEmptyFile(Path.Join(temp.Location, randomName));
            CreateEmptyFile(Path.Join(temp.Location, "dir", "a"));
            CreateEmptyFile(Path.Join(temp.Location, "dir", randomName));
            CreateEmptyFile(Path.Join(temp.Location, "dir2", "a"));
            CreateEmptyFile(Path.Join(Environment.CurrentDirectory, randomName));

            // No path separator
            // Not found
            Assert.Null(
                SearchPathSearcher.FindFile(randomName, false, null));
            // Found from the search path
            Assert.Equal(
                Path.Join(temp.Location, randomName),
                SearchPathSearcher.FindFile(randomName, false, searchPath));
            // Found from the current directory
            Assert.Equal(
                Path.Join(Environment.CurrentDirectory, randomName),
                SearchPathSearcher.FindFile(randomName, true, null));
            Assert.Equal(
                Path.Join(Environment.CurrentDirectory, randomName),
                SearchPathSearcher.FindFile(randomName, true, searchPath));

            // Relative path
            Assert.Equal(
                Path.Join(Environment.CurrentDirectory, ".", randomName),
                SearchPathSearcher.FindFile(Path.Join(".", randomName), true, searchPath));
            Assert.Null(
                SearchPathSearcher.FindFile(Path.Join(".", randomName), false, searchPath));

            // Absolute path
            AssertEqualForAllMethods(
                Path.Join(temp.Location, randomName),
                Path.Join(temp.Location, randomName),
                searchPath);
            AssertEqualForAllMethods(
                Path.Join(temp.Location, "dir2", randomName),
                Path.Join(temp.Location, "dir2", randomName),
                searchPath);

            static void AssertEqualForAllMethods(string? expected, string fileName, IReadOnlyList<string> searchPath)
            {
                Assert.Equal(expected, SearchPathSearcher.FindFile(fileName, false, null));
                Assert.Equal(expected, SearchPathSearcher.FindFile(fileName, true, null));
                Assert.Equal(expected, SearchPathSearcher.FindFile(fileName, false, searchPath));
                Assert.Equal(expected, SearchPathSearcher.FindFile(fileName, true, searchPath));
            }
        }

        [Fact]
        public void AppendsExeExtensionOnWindows()
        {
            using var temp = new TemporaryDirectory();
            var searchPath = new string[] { temp.Location };

            CreateEmptyFile(Path.Join(temp.Location, "a"));
            CreateEmptyFile(Path.Join(temp.Location, "a.exe"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(
                    Path.Join(temp.Location, "a.exe"),
                    SearchPathSearcher.FindExecutable("a", false, searchPath));
                Assert.Equal(
                    Path.Join(temp.Location, "a."),
                    SearchPathSearcher.FindExecutable("a.", false, searchPath));
            }
            else
            {
                Assert.Equal(
                    Path.Join(temp.Location, "a"),
                    SearchPathSearcher.FindExecutable("a", false, searchPath));
            }
        }

        [Fact]
        public void CanSplitPathEnvironmentVariable()
        {
            AssertResult(Array.Empty<string>(), null);
            AssertResult(Array.Empty<string>(), "");
            AssertResult(Array.Empty<string>(), ";;;");
            AssertResult(new string[] { "a", "b" }, "a;b");
            AssertResult(new string[] { "a", "b" }, ";a;b;");
            AssertResult(new string[] { "/a/b/c", @"\a\b\c" }, @";/a/b/c;\a\b\c;");

            static void AssertResult(IReadOnlyList<string> expected, string? argument)
            {
                var actual = SearchPathSearcher.ResolveSearchPath(argument?.Replace(';', Path.PathSeparator));

                Assert.Equal(GetPrependedSearchPathFragments().Concat(expected), actual);
            }
        }

        private static string[] GetPrependedSearchPathFragments()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.System, Environment.SpecialFolderOption.DoNotVerify),
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows, Environment.SpecialFolderOption.DoNotVerify),
                };
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        private static void CreateEmptyFile(string path) => File.WriteAllBytes(path, Array.Empty<byte>());
    }
}
