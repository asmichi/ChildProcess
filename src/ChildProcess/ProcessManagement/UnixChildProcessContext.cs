// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class UnixChildProcessContext : IChildProcessContext
    {
        // NOTE: Make sure to sync with the helper.
        private const uint RequestFlagsRedirectStdin = 1U << 0;
        private const uint RequestFlagsRedirectStdout = 1U << 1;
        private const uint RequestFlagsRedirectStderr = 1U << 2;
        private const uint RequestFlagsCreateNewProcessGroup = 1 << 3;
        private const uint RequestFlagsEnableAutoTermination = 1 << 4;
        private const int InitialBufferCapacity = 256; // Minimal capacity that every practical request will consume.

        private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        private readonly UnixHelperProcess _helperProcess;
        private readonly Task _readNotificationsTask;

        internal UnixChildProcessContext()
            : this(Environment.ProcessorCount)
        {
        }

        public UnixChildProcessContext(int maxSubchannelCount)
        {
            _helperProcess = UnixHelperProcess.Launch(maxSubchannelCount);
            _readNotificationsTask = ReadNotificationsAsync(_shutdownTokenSource.Token);
        }

        public void Dispose()
        {
            if (_readNotificationsTask.IsCompleted)
            {
                _shutdownTokenSource.Dispose();
            }
            else
            {
                _shutdownTokenSource.Cancel();
            }

            _helperProcess.Dispose();
        }

        public Task ShutdownAsync()
        {
            _shutdownTokenSource.Cancel();
            return _readNotificationsTask;
        }

        public IChildProcessStateHolder SpawnProcess(
            ref ChildProcessStartInfoInternal startInfo,
            string resolvedPath,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            var arguments = startInfo.Arguments;
            var environmentVariables = startInfo.EnvironmentVariables;
            var workingDirectory = startInfo.WorkingDirectory;

            Span<int> fds = stackalloc int[3];
            int handleCount = 0;
            uint flags = 0;
            if (stdIn != null)
            {
                fds[handleCount++] = stdIn.DangerousGetHandle().ToInt32();
                flags |= RequestFlagsRedirectStdin;
            }
            if (stdOut != null)
            {
                fds[handleCount++] = stdOut.DangerousGetHandle().ToInt32();
                flags |= RequestFlagsRedirectStdout;
            }
            if (stdErr != null)
            {
                fds[handleCount++] = stdErr.DangerousGetHandle().ToInt32();
                flags |= RequestFlagsRedirectStderr;
            }

            if (startInfo.CreateNewConsole)
            {
                flags |= RequestFlagsCreateNewProcessGroup;
            }
            else
            {
                Debug.Assert(!startInfo.AllowSignal);
            }

            // If AttachToCurrentConsole (== !startInfo.AllowSignal), leave the process running after we (the parent) exit.
            // After being orphaned (and possibly reparented to the shell), it may continue running or may be terminated by SIGTTIN/SIGTTOU.
            if (startInfo.AllowSignal)
            {
                flags |= RequestFlagsEnableAutoTermination;
            }

            using var bw = new MyBinaryWriter(InitialBufferCapacity);
            var stateHolder = UnixChildProcessState.Create(this, startInfo.AllowSignal);
            try
            {
                bw.Write(stateHolder.State.Token);
                bw.Write(flags);
                bw.Write(workingDirectory);
                bw.Write(resolvedPath);

                bw.Write((uint)(arguments.Count + 1));
                bw.Write(resolvedPath);
                foreach (var x in arguments)
                {
                    bw.Write(x);
                }

                if (environmentVariables == null)
                {
                    bw.Write(0U);
                }
                else
                {
                    bw.Write((uint)environmentVariables.Count);
                    foreach (var (name, value) in environmentVariables)
                    {
                        bw.WriteEnvironmentVariable(name, value);
                    }
                }

                // Work around https://github.com/microsoft/WSL/issues/6490
                // On WSL 1, if you call recvmsg multiple times to fully receive data sent with sendmsg,
                // the fds will be duplicated for each recvmsg call.
                // Send only fixed length of of data with the fds and receive that much data with one recvmsg call.
                // That will be safer anyway.
                Span<byte> header = stackalloc byte[sizeof(uint) * 2];
                if (!BitConverter.TryWriteBytes(header, (uint)UnixHelperProcessCommand.SpawnProcess)
                    || !BitConverter.TryWriteBytes(header.Slice(sizeof(uint)), bw.Length))
                {
                    Debug.Fail("Should never fail.");
                }

                var subchannel = _helperProcess.RentSubchannelAsync(default).AsTask().GetAwaiter().GetResult();

                try
                {
                    subchannel.SendExactBytesAndFds(header, fds.Slice(0, handleCount));

                    subchannel.SendExactBytes(bw.GetBuffer());
                    GC.KeepAlive(stdIn);
                    GC.KeepAlive(stdOut);
                    GC.KeepAlive(stdErr);

                    var (error, pid) = subchannel.ReceiveCommonResponse();
                    if (error > 0)
                    {
                        throw new Win32Exception(error);
                    }
                    else if (error < 0)
                    {
                        throw new AsmichiChildProcessInternalLogicErrorException(
                            string.Format(CultureInfo.InvariantCulture, "Internal logic error: Bad request {0}.", error));
                    }

                    stateHolder.State.SetPid(pid);

                    return stateHolder;
                }
                finally
                {
                    _helperProcess.ReturnSubchannel(subchannel);
                }
            }
            catch
            {
                stateHolder.Dispose();
                throw;
            }
        }

        public void SendSignal(long token, UnixHelperProcessSignalNumber signalNumber)
        {
            Span<byte> request = stackalloc byte[4 + 4 + 8 + 4];
            if (!BitConverter.TryWriteBytes(request, (uint)UnixHelperProcessCommand.SignalProcess)
                || !BitConverter.TryWriteBytes(request.Slice(4), 8 + 4)
                || !BitConverter.TryWriteBytes(request.Slice(4 + 4), token)
                || !BitConverter.TryWriteBytes(request.Slice(4 + 4 + 8), (uint)signalNumber))
            {
                Debug.Fail("Should never fail.");
            }

            var subchannel = _helperProcess.RentSubchannelAsync(default).AsTask().GetAwaiter().GetResult();
            try
            {
                subchannel.SendExactBytes(request);

                var (error, pid) = subchannel.ReceiveCommonResponse();
                if (error > 0)
                {
                    throw new Win32Exception(error);
                }
                else if (error < 0)
                {
                    throw new AsmichiChildProcessInternalLogicErrorException(
                        string.Format(CultureInfo.InvariantCulture, "Internal logic error: Bad request {0}.", error));
                }
            }
            finally
            {
                _helperProcess.ReturnSubchannel(subchannel);
            }
        }

        private async Task ReadNotificationsAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(Marshal.SizeOf<ChildExitNotification>() == ChildExitNotification.Size);
            int carriedOverBytes = 0;
            var buf = new byte[256];
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytes = carriedOverBytes + await _helperProcess.ReadFromMainChannelAsync(buf.AsMemory(carriedOverBytes), cancellationToken).ConfigureAwait(false);
                int elementCount = bytes / ChildExitNotification.Size;
                for (int i = 0; i < elementCount; i++)
                {
                    ProcessNotification(ref MemoryMarshal.AsRef<ChildExitNotification>(buf.AsSpan(i * ChildExitNotification.Size, ChildExitNotification.Size)));
                }

                carriedOverBytes = bytes % ChildExitNotification.Size;
                if (carriedOverBytes != 0)
                {
                    Array.Copy(buf, bytes - carriedOverBytes, buf, 0, carriedOverBytes);
                }
            }
        }

        private static void ProcessNotification(ref ChildExitNotification notification)
        {
            if (!UnixChildProcessState.TryGetChildProcessState(notification.Token, out var holder))
            {
                // Ignore. This is a process where exec failed and we already reported that failure.
                return;
            }

            using (holder)
            {
                if (holder.State.HasExitCode)
                {
                    Trace.WriteLine("warning: Multiple ChildExitNotification delivered to one process.");
                }
                else
                {
                    holder.State.SetExited(notification.Status);
                }
            }
        }

        // NOTE: Make sure to sync with the server.
        [StructLayout(LayoutKind.Sequential)]
        private struct ChildExitNotification
        {
            public const int Size = 16;

            public long Token;
            public int ProcessID;
            public int Status;
        }
    }
}
