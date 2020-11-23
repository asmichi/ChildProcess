// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable SA1310 // Field names must not contain underscore

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class Kernel32
    {
        public const uint PIPE_ACCESS_INBOUND = 1;
        public const uint PIPE_ACCESS_OUTBOUND = 2;
        public const uint PIPE_ACCESS_DUPLEX = 3;
        public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const uint PIPE_TYPE_BYTE = 0;
        public const uint PIPE_READMODE_BYTE = 0;
        public const uint PIPE_WAIT = 0;
        public const uint PIPE_REJECT_REMOTE_CLIENTS = 8;

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CreatePipe(
            [Out] out SafeFileHandle hReadPipe,
            [Out] out SafeFileHandle hWritePipe,
            [In] IntPtr lpPipeAttributes,
            [In] int nSize);

        [DllImport(DllName, EntryPoint = "CreateNamedPipeW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        public static extern SafeFileHandle CreateNamedPipe(
            [In] string lpName,
            [In] uint dwOpenMode,
            [In] uint dwPipeMode,
            [In] uint nMaxInstances,
            [In] uint nOutBufferSize,
            [In] uint nInBufferSize,
            [In] uint nDefaultTimeOut,
            [In] IntPtr lpSecurityAttributes);
    }
}
