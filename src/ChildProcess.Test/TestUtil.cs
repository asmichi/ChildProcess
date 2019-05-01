// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;

namespace Asmichi.Utilities
{
    internal static class TestUtil
    {
        public static string DotnetCommand => "dotnet";
        public static string TestChildPath => Path.Combine(Environment.CurrentDirectory, "TestChild.dll");
    }

    internal sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            this.Location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Location);
        }

        public void Dispose()
        {
            Directory.Delete(Location, true);
        }

        public string Location { get; }
    }
}
