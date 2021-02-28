// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;

namespace Asmichi.Interop.Windows
{
    internal static partial class Kernel32
    {
        [DllImport(DllName)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport(DllName, SetLastError = true)]
        public static extern bool GetConsoleMode([In] SafeHandle hConsoleHandle, [Out] out int lpMode);

        // Not using SafeHandle; we do not own the returned handle.
        [DllImport(DllName, SetLastError = true)]
        public static extern IntPtr GetStdHandle([In] int nStdHandle);

        // return: HRESULT
        [DllImport(DllName)]
        internal static extern int CreatePseudoConsole(
            [In] COORD size,
            [In] SafeHandle hInput,
            [In] SafeHandle hOutput,
            [In] uint dwFlags,
            [Out] out IntPtr phPC);

        // return: HRESULT
        [DllImport(DllName)]
        internal static extern int ResizePseudoConsole([In] IntPtr hPC, [In] COORD size);

        [DllImport(DllName)]
        internal static extern void ClosePseudoConsole([In] IntPtr hPC);
    }
}
