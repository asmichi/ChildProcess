// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable SA1310 // Field names must not contain underscore

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class Kernel32
    {
        public const int ERROR_FILE_NOT_FOUND = 2;
        public const int ERROR_PIPE_BUSY = 231;

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_SHARE_DELETE = 0x00000004;

        public const int CREATE_NEW = 1;
        public const int CREATE_ALWAYS = 2;
        public const int OPEN_EXISTING = 3;
        public const int OPEN_ALWAYS = 4;
        public const int TRUNCATE_EXISTING = 5;

        public const int FILE_TYPE_UNKNOWN = 0x0000;
        public const int FILE_TYPE_CHAR = 0x0002;

        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;

        public const int HANDLE_FLAG_INHERIT = 0x00000001;
        public const int DUPLICATE_SAME_ACCESS = 0x00000002;

        private const string DllName = "kernel32.dll";

        [DllImport(DllName, EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        public static extern SafeFileHandle CreateFile(
            [In] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] uint dwShareMode,
            [In] IntPtr lpSecurityAttributes,
            [In] int dwCreationDisposition,
            [In] int dwFlagsAndAttributes,
            [In] IntPtr hTemplateFile);

        [DllImport(DllName, SetLastError = true)]
        public static extern int GetFileType([In] SafeHandle hFile);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool GetHandleInformation([In] SafeHandle hObject, out int lpdwFlags);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool DuplicateHandle(
            [In] IntPtr hSourceProcessHandle, // We pass only the current process to this.
            [In] SafeHandle hSourceHandle,
            [In] IntPtr hTargetProcessHandle, // We pass only the current process to this.
            [Out] out SafeAnyHandle lpTargetHandle,
            [In] int dwDesiredAccess,
            [In] bool bInheritHandle,
            [In] int dwOptions);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool DuplicateHandle(
            [In] IntPtr hSourceProcessHandle, // We pass only the current process to this.
            [In] SafeHandle hSourceHandle,
            [In] IntPtr hTargetProcessHandle, // We pass only the current process to this.
            [Out] out SafeWaitHandle lpTargetHandle,
            [In] int dwDesiredAccess,
            [In] bool bInheritHandle,
            [In] int dwOptions);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CloseHandle([In] IntPtr handle);

        [DllImport(DllName)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport(DllName)]
        public static extern uint GetCurrentProcessId();

        [DllImport(DllName)]
        public static extern bool GetExitCodeProcess(
            [In] SafeProcessHandle hProcess,
            [Out] out int lpExitCode);

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;
        }
    }
}
