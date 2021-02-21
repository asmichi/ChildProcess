// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asmichi.Utilities.Interop.Linux;
using Asmichi.Utilities.PlatformAbstraction.Unix;
using static System.FormattableString;

namespace Asmichi.Utilities.ProcessManagement
{
    // NOTE: Make sure to sync with the helper.
    internal enum UnixHelperProcessCommand : uint
    {
        SpawnProcess = 0,
        SignalProcess = 1,
    }

    // NOTE: Make sure to sync with the helper.
    internal enum UnixHelperProcessErrorCode : int
    {
        InvalidRequest = -1,
        RequestTooBig = -2,
    }

    // NOTE: Make sure to sync with the helper.
    internal enum UnixHelperProcessSignalNumber : uint
    {
        Interrupt = 2,
        Kill = 9,
        Termination = 15,
    }

    internal sealed class UnixHelperProcess : IDisposable
    {
        private const string HelperFileName = "AsmichiChildProcessHelper";
        private const int HelperConnectionPollingCount = 600;
        private const int HelperConnectionPollingIntervalMicroSeconds = 100 * 1000;
        private static readonly string HelperPath = GetHelperPath();
        private static readonly ReadOnlyMemory<byte> HelperHello = new byte[4] { 0x41, 0x53, 0x4d, 0x43 };

        private readonly Process _helperProcess;
        private readonly Socket _mainChannelSocket;
        private readonly NetworkStream _mainChannel;
        private readonly int _maxSubchannelCount;
        private readonly Channel<UnixSubchannel> _subchannels;
        private bool _isDisposed;
        private int _subchannelCount;

        public UnixHelperProcess(Process helperProcess, Socket mainChannel, int maxSubchannelCount)
        {
            _helperProcess = helperProcess;
            _mainChannelSocket = mainChannel;
            _mainChannel = new NetworkStream(mainChannel, ownsSocket: true);
            _maxSubchannelCount = maxSubchannelCount;
            _subchannels = Channel.CreateUnbounded<UnixSubchannel>();
        }

        public void Dispose()
        {
            Debug.Assert(!_isDisposed, "Internal call sites should avoid double-dispose.");

            if (!_isDisposed)
            {
                _ = _subchannels.Writer.TryComplete();
                while (_subchannels.Reader.TryRead(out var subchannel))
                {
                    subchannel.Dispose();
                }

                _mainChannel.Dispose();
                _helperProcess.Dispose();

                _isDisposed = true;
            }
        }

        public ValueTask<int> ReadFromMainChannelAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _mainChannel.ReadAsync(buffer, cancellationToken);

        public ValueTask<UnixSubchannel> RentSubchannelAsync(CancellationToken cancellationToken)
        {
            // Check if a subchannel is available.
            if (_subchannels.Reader.TryRead(out var subchannel))
            {
                return new ValueTask<UnixSubchannel>(subchannel);
            }

            // Create one if we have not reached _maxSubchannelCount yet.
            if (TryAddSubchannel(out subchannel))
            {
                return new ValueTask<UnixSubchannel>(subchannel);
            }

            // If we have reached _maxSubchannelCount, wait for one to be returned.
            return _subchannels.Reader.ReadAsync(cancellationToken);
        }

        private bool TryAddSubchannel([NotNullWhen(true)] out UnixSubchannel? subchannel)
        {
            CheckNotDisposed();

            if (!TryIncrementSubchannelCount())
            {
                subchannel = default;
                return false;
            }

            subchannel = CreateSubchannel();
            return true;
        }

        private bool TryIncrementSubchannelCount()
        {
            while (true)
            {
                int initialCount = _subchannelCount;
                if (initialCount > _maxSubchannelCount)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref _subchannelCount, initialCount + 1, initialCount) == initialCount)
                {
                    return true;
                }
            }
        }

        private UnixSubchannel CreateSubchannel()
        {
            CheckNotDisposed();

            var mainChannelhandle = _mainChannelSocket.SafeHandle;
            var subchannelHandle = LibChildProcess.SubchannelCreate(mainChannelhandle.DangerousGetHandle());
            if (subchannelHandle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new UnixSubchannel(subchannelHandle);
        }

        public void ReturnSubchannel(UnixSubchannel subchannel)
        {
            // Succeeds unless _subchannels has been completed.
            _ = _subchannels.Writer.TryWrite(subchannel);
        }

        // Guard against use of unmanaged resources after disposal.
        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChildProcessImpl));
            }
        }

        public static UnixHelperProcess Launch(int maxSubchannelCount)
        {
            if (maxSubchannelCount < 1)
            {
                throw new ArgumentException("maxSubchannelCount must be greater than 0.", nameof(maxSubchannelCount));
            }

            var pipePath = UnixFilePal.CreateUniqueSocketPath();

            using var listeningSocket = UnixFilePal.CreateListeningDomainSocket(pipePath, 1);
            var psi = new ProcessStartInfo(HelperPath, Invariant($"\"{pipePath}\""))
            {
                RedirectStandardInput = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = false,
            };

            var process = Process.Start(psi);
            process.StandardInput.Close();

            var mainChannel = WaitForConnection(process, listeningSocket);

            return new UnixHelperProcess(process, mainChannel, maxSubchannelCount);

            static Socket WaitForConnection(Process process, Socket listeningSocket)
            {
                Span<byte> helloBuf = stackalloc byte[HelperHello.Length];

                for (int i = 0; i < HelperConnectionPollingCount; i++)
                {
                    if (listeningSocket.Poll(HelperConnectionPollingIntervalMicroSeconds, SelectMode.SelectRead))
                    {
                        var socket = listeningSocket.Accept();
                        int bytes = socket.Receive(helloBuf);
                        if (bytes != HelperHello.Length)
                        {
                            socket.Dispose();
                            continue;
                        }
                        return socket;
                    }

                    if (process.WaitForExit(0))
                    {
                        throw new AsmichiChildProcessLibraryCrashedException(CurrentCulture($"The helper process died with exit code {process.ExitCode}."));
                    }
                }

                throw new AsmichiChildProcessInternalLogicErrorException("The helper process did not connect to this process.");
            }
        }

        private static unsafe string GetHelperPath()
        {
            int requiredBufLen = LibChildProcess.GetDllPath(null, 0);
            if (requiredBufLen < 0)
            {
                throw new AsmichiChildProcessInternalLogicErrorException();
            }

            var buf = new StringBuilder(requiredBufLen);
            if (LibChildProcess.GetDllPath(buf, buf.Capacity) < 0)
            {
                throw new AsmichiChildProcessInternalLogicErrorException();
            }

            return Path.Combine(Path.GetDirectoryName(buf.ToString())!, HelperFileName);
        }
    }
}
