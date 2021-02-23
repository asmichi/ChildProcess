// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;

namespace Asmichi.Utilities
{
    internal static class TestUtil
    {
        public static string DotnetCommandName => "dotnet";
        public static string TestChildPath => Path.Combine(Environment.CurrentDirectory, "TestChild.dll");
        public static string TestChildNativePath => Path.Combine(Environment.CurrentDirectory, "TestChildNative");
    }
}
