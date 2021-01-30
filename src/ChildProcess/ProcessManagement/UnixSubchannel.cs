// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Asmichi.Utilities.Interop.Linux;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class UnixSubchannel : IDisposable
    {
        private readonly SafeSocketHandle _handle;

        public UnixSubchannel(SafeSocketHandle handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        public unsafe void SendExactBytes(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                if (!LibChildProcess.SubchannelSendExactBytes(
                    _handle, pBuffer, new UIntPtr((uint)buffer.Length)))
                {
                    var err = Marshal.GetLastWin32Error();
                    ThrowFatalCommnicationError(err);
                }
            }
        }

        public unsafe void SendExactBytesAndFds(ReadOnlySpan<byte> buffer, ReadOnlySpan<int> fds)
        {
            fixed (byte* pBuffer = buffer)
            {
                fixed (int* pFds = fds)
                {
                    if (!LibChildProcess.SubchannelSendExactBytesAndFds(
                        _handle, pBuffer, new UIntPtr((uint)buffer.Length), pFds, new UIntPtr((uint)fds.Length)))
                    {
                        var err = Marshal.GetLastWin32Error();
                        ThrowFatalCommnicationError(err);
                    }
                }
            }
        }

        public unsafe (int error, int pid) ReadResponse()
        {
            const int ResponseInts = 2;
            fixed (int* pBuf = stackalloc int[ResponseInts])
            {
                if (!LibChildProcess.SubchannelRecvExactBytes(_handle, pBuf, new UIntPtr(ResponseInts * sizeof(int))))
                {
                    var err = Marshal.GetLastWin32Error();
                    ThrowFatalCommnicationError(err);
                }

                return (pBuf[0], pBuf[1]);
            }
        }

        [DoesNotReturn]
        private static void ThrowFatalCommnicationError(int err)
        {
            if (err == 0)
            {
                // TODO: handle helper crash or internal communication error
                throw new AsmichiChildProcessLibraryCrashedException("Process cannot be created: Connection reset.");
            }
            else
            {
                throw new Win32Exception(err);
            }
        }
    }
}
