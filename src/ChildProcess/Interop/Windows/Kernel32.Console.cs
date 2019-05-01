// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class Kernel32
    {
        [DllImport(DllName)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport(DllName, SetLastError = true)]
        public static extern bool GetConsoleMode([In] SafeHandle hConsoleHandle, [Out] out int lpMode);

        // Not using SafeHandle; we do not own the returned handle.
        [DllImport(DllName, SetLastError = true)]
        public static extern IntPtr GetStdHandle([In]int nStdHandle);
    }
}
