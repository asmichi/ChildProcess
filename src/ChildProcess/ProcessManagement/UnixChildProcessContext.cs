// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class UnixChildProcessContext : IChildProcessContext
    {
        private const uint RequestFlagsRedirectStdin = 1U << 0;
        private const uint RequestFlagsRedirectStdout = 1U << 1;
        private const uint RequestFlagsRedirectStderr = 1U << 2;
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
            string fileName,
            IReadOnlyCollection<string> arguments,
            string? workingDirectory,
            IReadOnlyCollection<KeyValuePair<string, string>>? environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
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

            using var bw = new MyBinaryWriter(InitialBufferCapacity);
            var stateHolder = UnixChildProcessState.Create();
            try
            {
                bw.Write(0U); // Dummy length to be rewritten later
                bw.Write(stateHolder.State.Token);
                bw.Write(flags);
                bw.Write(workingDirectory);
                bw.Write(fileName);

                bw.Write((uint)(arguments.Count + 1));
                bw.Write(fileName);
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

                bw.RewritePrefixedLength();

                var subchannel = _helperProcess.RentSubchannelAsync(default).AsTask().GetAwaiter().GetResult();

                try
                {
                    subchannel.SendExactBytesAndFds(bw.GetBuffer(), fds.Slice(0, handleCount));
                    GC.KeepAlive(stdIn);
                    GC.KeepAlive(stdOut);
                    GC.KeepAlive(stdErr);

                    var (error, pid) = subchannel.ReadResponse();
                    if (error > 0)
                    {
                        throw new Win32Exception(error);
                    }
                    else if (error < 0)
                    {
                        throw new AsmichiChildProcessInternalLogicErrorException(
                            string.Format(CultureInfo.InvariantCulture, "Internal logic error: Bad request {0}.", -error));
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
    }
}
