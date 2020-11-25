// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using Asmichi.Utilities.Interop.Linux;
using Asmichi.Utilities.PlatformAbstraction.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Unix
{
    internal sealed class UnixFilePal : IFilePal
    {
        private static readonly string SocketPathPrefix = MakeSocketPathPrefix();
        private static int pipeSerialNumber;

        public SafeFileHandle OpenNullDevice(FileAccess fileAccess)
        {
            var fd = LibChildProcess.OpenNullDevice(ToLibChildProcessFileAccess(fileAccess));
            if (fd.IsInvalid)
            {
                throw new Win32Exception();
            }

            return fd;
        }

        private static int ToLibChildProcessFileAccess(FileAccess fileAccess)
        {
            return (((fileAccess & FileAccess.Read) != 0) ? LibChildProcess.FileAccessRead : 0)
                | (((fileAccess & FileAccess.Write) != 0) ? LibChildProcess.FileAccessWrite : 0);
        }

        public (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
            if (!LibChildProcess.CreatePipe(out var readPipe, out var writePipe))
            {
                throw new Win32Exception();
            }

            return (readPipe, writePipe);
        }

        public (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection)
        {
            var pipePath = CreateUniqueSocketPath();

            try
            {
                using var listeningSock = CreateListeningDomainSocket(pipePath, 1);

                // We do not want a Socket to manage and close this fd; connect on our own.
                if (!LibChildProcess.ConnectToUnixSocket(pipePath, out var clientSock))
                {
                    throw new Win32Exception();
                }
                Debug.Assert(listeningSock.Poll(0, SelectMode.SelectRead));

                var serverSock = listeningSock.Accept();
                var serverStream = new NetworkStream(serverSock, ownsSocket: true);
                return (serverStream, clientSock);
            }
            finally
            {
                File.Delete(pipePath);
            }
        }

        public static Socket CreateListeningDomainSocket(string path, int backlog)
        {
            File.Delete(path);

            Socket? listeningSock = null;
            try
            {
                // NOTE: Socket does not have a public ctor taking fd, which prevents us from using socketpair to create a Socket.
                //       Create a listening Socket and connect to it in order to create a connected unix socket pair.
                listeningSock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listeningSock.Bind(new UnixDomainSocketEndPoint(path));
                listeningSock.Listen(backlog);
                return listeningSock;
            }
            catch
            {
                listeningSock?.Dispose();
                // This may fail, but in such a racy situation it is okay to pretend the previous File.Delete operation failed.
                File.Delete(path);
                throw;
            }
        }

        public static string CreateUniqueSocketPath()
        {
            var thisPipeSerialNumber = Interlocked.Increment(ref pipeSerialNumber);
            return SocketPathPrefix + thisPipeSerialNumber.ToString(CultureInfo.InvariantCulture);
        }

        private static string MakeSocketPathPrefix()
        {
            var candidate = NamedPipeUtil.MakePipePathPrefix(Path.GetTempPath(), (uint)LibChildProcess.GetPid());
            const int maxBodyLength = 11;
            if ((ulong)(candidate.Length + maxBodyLength) > LibChildProcess.GetMaxSocketPathLength().ToUInt64())
            {
                throw new PathTooLongException("Path to the temporary directory is too long to accomodate a socket file.");
            }

            return candidate;
        }
    }
}
