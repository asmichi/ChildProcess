// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

namespace Asmichi.Utilities
{
    internal static class Kernel32
    {
        private const string DllName = "kernel32.dll";

        [DllImport(DllName, SetLastError = true)]
        public static extern int GetConsoleOutputCP();
    }
}
