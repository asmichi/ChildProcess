// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class UnixChildProcessContext : IChildProcessContext
    {
        // NOTE: Make sure to sync with the helper.
        private const uint RequestFlagsRedirectStdin = 1U << 0;
        private const uint RequestFlagsRedirectStdout = 1U << 1;
        private const uint RequestFlagsRedirectStderr = 1U << 2;
        private const uint RequestFlagsCreateNewProcessGroup = 1 << 3;
        private const int InitialBufferCapacity = 256; // Minimal capacity that every practical request will consume.

        private readonly UnixHelperProcess _helperProcess;

        internal UnixChildProcessContext()
            : this(Environment.ProcessorCount)
        {
        }

        public UnixChildProcessContext(int maxSubchannelCount)
        {
            _helperProcess = UnixHelperProcess.Launch(maxSubchannelCount);
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
    }
}
