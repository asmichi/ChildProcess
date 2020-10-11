// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.PlatformAbstraction.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal sealed class WindowsFilePal : IFilePal
    {
        private const string NullDeviceFileName = "NUL";
        private static readonly string PipePathPrefix = NamedPipeUtil.MakePipePathPrefix(@"\\.\pipe", Kernel32.GetCurrentProcessId());
        private static int pipeSerialNumber;

        public SafeFileHandle OpenNullDevice(FileAccess fileAccess)
        {
            return OpenFile(NullDeviceFileName, fileAccess);
        }

        private static SafeFileHandle OpenFile(
            string fileName,
            FileAccess fileAccess)
        {
            var handle = Kernel32.CreateFile(
                fileName,
                ToNativeDesiredAccess(fileAccess),
                Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE | Kernel32.FILE_SHARE_DELETE,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw new Win32Exception();
            }

            return handle;
        }

        private static uint ToNativeDesiredAccess(FileAccess fileAccess)
        {
            return (((fileAccess & FileAccess.Read) != 0) ? Kernel32.GENERIC_READ : 0)
                & (((fileAccess & FileAccess.Write) != 0) ? Kernel32.GENERIC_WRITE : 0);
        }

        public (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
            if (!Kernel32.CreatePipe(out var readPipe, out var writePipe, IntPtr.Zero, 0))
            {
                throw new Win32Exception();
            }

            return (readPipe, writePipe);
        }

        public (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection)
        {
            var (serverMode, clientMode) = ToModes(pipeDirection);

            while (true)
            {
                // Make a unique name of a named pipe to create.
                var thisPipeSerialNumber = Interlocked.Increment(ref pipeSerialNumber);
                var pipeName = PipePathPrefix + thisPipeSerialNumber.ToString(CultureInfo.InvariantCulture);

                var serverPipe = Kernel32.CreateNamedPipe(
                    pipeName,
                    serverMode | Kernel32.FILE_FLAG_OVERLAPPED | Kernel32.FILE_FLAG_FIRST_PIPE_INSTANCE,
                    Kernel32.PIPE_TYPE_BYTE | Kernel32.PIPE_READMODE_BYTE | Kernel32.PIPE_WAIT | Kernel32.PIPE_REJECT_REMOTE_CLIENTS,
                    1,
                    4096,
                    4096,
                    0,
                    IntPtr.Zero);
                if (serverPipe.IsInvalid)
                {
                    throw new Win32Exception();
                }

                var clientPipe = Kernel32.CreateFile(
                    pipeName,
                    clientMode,
                    0,
                    IntPtr.Zero,
                    Kernel32.OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (clientPipe.IsInvalid)
                {
                    var lastError = Marshal.GetLastWin32Error();

                    serverPipe.Dispose();

                    if (lastError == Kernel32.ERROR_PIPE_BUSY)
                    {
                        // The pipe has been stolen by an unrelated process; retry creation.
                        continue;
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }

                try
                {
                    var streamAccess = pipeDirection == PipeDirection.In ? FileAccess.Read : FileAccess.Write;
                    var serverStream = new FileStream(serverPipe, streamAccess, 4096, isAsync: true);

                    return (serverStream, clientPipe);
                }
                catch
                {
                    serverPipe.Dispose();
                    clientPipe.Dispose();
                    throw;
                }
            }
        }

        private static (uint serverMode, uint clientMode) ToModes(PipeDirection pipeDirection)
        {
            switch (pipeDirection)
            {
                case PipeDirection.In:
                    return (Kernel32.PIPE_ACCESS_INBOUND, Kernel32.GENERIC_WRITE);
                case PipeDirection.Out:
                    return (Kernel32.PIPE_ACCESS_OUTBOUND, Kernel32.GENERIC_READ);
                case PipeDirection.InOut:
                default:
                    throw new ArgumentException("Must be PipeDirection.In or PipeDirection.Out", nameof(pipeDirection));
            }
        }
    }
}
