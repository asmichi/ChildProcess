// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Interop.Linux
{
    internal static class LibChildProcess
    {
        private const string DllName = "libAsmichiChildProcess";

        public const int FileAccessRead = 1;
        public const int FileAccessWrite = 2;

        [DllImport(DllName, SetLastError = false, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool ConnectToUnixSocket(
            [In] string path,
            [Out] out SafeFileHandle sock);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CreatePipe(
            [Out] out SafeFileHandle readPipe,
            [Out] out SafeFileHandle writePipe);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool DuplicateStdFileForChild(
            [In] int stdFd,
            [In] bool createNewProcessGroup,
            [Out] out SafeFileHandle outFd);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CreateUnixStreamSocketPair(
            [Out] out SafeFileHandle sock1,
            [Out] out SafeFileHandle sock2);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetDllPath(
            [Out] StringBuilder? buf,
            [In] int len);

        [DllImport(DllName)]
        public static extern int GetENOENT();

        [DllImport(DllName, SetLastError = false)]
        public static extern nuint GetMaxSocketPathLength();

        [DllImport(DllName)]
        public static extern int GetPid();

        [DllImport(DllName, SetLastError = true)]
        public static extern SafeFileHandle OpenNullDevice(int fileAccess);

        [DllImport(DllName, SetLastError = true)]
        public static extern SafeSocketHandle SubchannelCreate(
            [In] IntPtr mainChannelFd);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool SubchannelDestroy(
            [In] IntPtr subchannelFd);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe bool SubchannelRecvExactBytes(
            [In] SafeSocketHandle subchannelFd,
            [In] void* buf,
            [In] nuint len);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe bool SubchannelSendExactBytes(
            [In] SafeSocketHandle subchannelFd,
            [In] void* buf,
            [In] nuint len);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe bool SubchannelSendExactBytesAndFds(
            [In] SafeSocketHandle subchannelFd,
            [In] void* buf,
            [In] nuint len,
            [In] int* fds,
            [In] nuint fdCount);
    }
}
