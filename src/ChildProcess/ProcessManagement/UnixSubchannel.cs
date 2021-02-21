// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        public unsafe (int error, int data) ReceiveCommonResponse()
        {
            const int ResponseInts = 2;
            fixed (int* pBuffer = stackalloc int[ResponseInts])
            {
                RecvExactBytes(pBuffer, ResponseInts * sizeof(int));
                return (pBuffer[0], pBuffer[1]);
            }
        }

        public unsafe void SendExactBytes(ReadOnlySpan<byte> buffer)
        {
            CheckNotDisposed();

            fixed (byte* pBuffer = buffer)
            {
                if (!LibChildProcess.SubchannelSendExactBytes(
                    _handle, pBuffer, (uint)buffer.Length))
                {
                    var err = Marshal.GetLastWin32Error();
                    ThrowFatalCommnicationError(err);
                }
            }
        }

        public unsafe void SendExactBytesAndFds(ReadOnlySpan<byte> buffer, ReadOnlySpan<int> fds)
        {
            CheckNotDisposed();

            fixed (byte* pBuffer = buffer)
            {
                fixed (int* pFds = fds)
                {
                    if (!LibChildProcess.SubchannelSendExactBytesAndFds(
                        _handle, pBuffer, (uint)buffer.Length, pFds, (uint)fds.Length))
                    {
                        var err = Marshal.GetLastWin32Error();
                        ThrowFatalCommnicationError(err);
                    }
                }
            }
        }

        private unsafe void RecvExactBytes(void* pBuf, uint length)
        {
            CheckNotDisposed();

            if (!LibChildProcess.SubchannelRecvExactBytes(_handle, pBuf, length))
            {
                var err = Marshal.GetLastWin32Error();
                ThrowFatalCommnicationError(err);
            }
        }

        // Guard against use of unmanaged resources after disposal.
        private void CheckNotDisposed()
        {
            if (_handle.IsClosed)
            {
                throw new ObjectDisposedException(nameof(ChildProcessImpl));
            }
        }

        [DoesNotReturn]
        private static void ThrowFatalCommnicationError(int err, [CallerMemberName] string? callerName = null)
        {
            if (err == 0)
            {
                throw new AsmichiChildProcessLibraryCrashedException("Process cannot be created: Connection reset.");
            }
            else
            {
                throw new AsmichiChildProcessInternalLogicErrorException(
                    string.Format(CultureInfo.InvariantCulture, "Internal Logic Error: Communication error in {0}: {1}", callerName, new Win32Exception(err).Message));
            }
        }
    }
}
