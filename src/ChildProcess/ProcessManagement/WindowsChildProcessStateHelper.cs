// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Asmichi.Interop.Windows;
using Asmichi.PlatformAbstraction;
using Asmichi.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    internal sealed class WindowsChildProcessStateHelper : IChildProcessStateHelper
    {
        private const int RetryIntervalMilliseconds = 10;
        private const int MaxRetryIntervalMilliseconds = 160;
        private const int MaxAttemptCount = 50;

        private static readonly string ChcpPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.System, Environment.SpecialFolderOption.DoNotVerify),
            "chcp.com");

        public void Dispose()
        {
        }

        public void ValidatePlatformSpecificStartInfo(in ChildProcessStartInfoInternal startInfo)
        {
        }

        public unsafe IChildProcessStateHolder SpawnProcess(
            ref ChildProcessStartInfoInternal startInfo,
            string resolvedPath,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            var arguments = startInfo.Arguments;
            var environmentVariables = startInfo.EnvironmentVariables;
            var workingDirectory = startInfo.WorkingDirectory;
            var flags = startInfo.Flags;

            Debug.Assert(startInfo.CreateNewConsole || ConsolePal.HasConsoleWindow());

            var commandLine = WindowsCommandLineUtil.MakeCommandLine(resolvedPath, arguments ?? Array.Empty<string>(), !flags.HasDisableArgumentQuoting());
            var environmentBlock = startInfo.UseCustomEnvironmentVariables ? WindowsEnvironmentBlockUtil.MakeEnvironmentBlock(environmentVariables.Span) : null;

            // Objects that need cleanup
            InputWriterOnlyPseudoConsole? pseudoConsole = null;
            SafeJobObjectHandle? jobObjectHandle = null;
            SafeProcessHandle? processHandle = null;
            SafeThreadHandle? threadHandle = null;

            try
            {
                pseudoConsole = startInfo.CreateNewConsole ? InputWriterOnlyPseudoConsole.Create() : null;
                if (pseudoConsole is not null && flags.HasUseCustomCodePage())
                {
                    ChangeCodePage(pseudoConsole, startInfo.CodePage, workingDirectory);
                }

                bool killOnClose = startInfo.AllowSignal && WindowsVersion.NeedsWorkaroundForWindows1809;
                jobObjectHandle = CreateJobObject(killOnClose, startInfo.DisableWindowsErrorReportingDialog);

                using var inheritableHandleStore = new InheritableHandleStore(3);
                var childStdIn = stdIn != null ? inheritableHandleStore.Add(stdIn) : null;
                var childStdOut = stdOut != null ? inheritableHandleStore.Add(stdOut) : null;
                var childStdErr = stdErr != null ? inheritableHandleStore.Add(stdErr) : null;

                IntPtr jobObjectHandles = jobObjectHandle.DangerousGetHandle();

                Span<IntPtr> inheritableHandles = stackalloc IntPtr[inheritableHandleStore.Count];
                inheritableHandleStore.DangerousGetHandles(inheritableHandles);
                fixed (IntPtr* pInheritableHandles = inheritableHandles)
                {
                    using var attr = new ProcThreadAttributeList(3);
                    if (pseudoConsole is not null)
                    {
                        attr.UpdatePseudoConsole(pseudoConsole.Handle.DangerousGetHandle());
                    }
                    attr.UpdateHandleList(pInheritableHandles, inheritableHandles.Length);
                    attr.UpdateJobList(&jobObjectHandles, 1);

                    const int CreationFlags =
                        Kernel32.CREATE_UNICODE_ENVIRONMENT
                        | Kernel32.EXTENDED_STARTUPINFO_PRESENT;

                    int processId;
                    (processId, processHandle, threadHandle) = InvokeCreateProcess(
                        commandLine,
                        CreationFlags,
                        environmentBlock,
                        workingDirectory,
                        childStdIn,
                        childStdOut,
                        childStdErr,
                        attr);

                    return new WindowsChildProcessState(processId, processHandle, threadHandle, jobObjectHandle, pseudoConsole, startInfo.AllowSignal, startInfo.Flags.HasEnableHandle());
                }
            }
            catch
            {
                if (processHandle is not null)
                {
                    Kernel32.TerminateProcess(processHandle, -1);
                    processHandle.Dispose();
                }
                threadHandle?.Dispose();
                pseudoConsole?.Dispose();
                jobObjectHandle?.Dispose();
                throw;
            }
        }

        // Change the code page of the specified pseudo console by invoking chcp.com on it.
        private static unsafe void ChangeCodePage(
            InputWriterOnlyPseudoConsole pseudoConsole,
            int codePage,
            string? workingDirectory)
        {
            var commandLine = new StringBuilder(ChcpPath.Length + 5);
            WindowsCommandLineUtil.AppendStringQuoted(commandLine, ChcpPath);
            commandLine.Append(' ');
            commandLine.Append(codePage.ToString(CultureInfo.InvariantCulture));

            using var inheritableHandleStore = new InheritableHandleStore(3);
            using var nullDevice = FilePal.OpenNullDevice(FileAccess.ReadWrite);
            var childStdIn = inheritableHandleStore.Add(nullDevice);
            var childStdOut = inheritableHandleStore.Add(nullDevice);
            var childStdErr = inheritableHandleStore.Add(nullDevice);

            SafeProcessHandle? processHandle = null;
            try
            {
                Span<IntPtr> inheritableHandles = stackalloc IntPtr[inheritableHandleStore.Count];
                inheritableHandleStore.DangerousGetHandles(inheritableHandles);
                fixed (IntPtr* pInheritableHandles = inheritableHandles)
                {
                    using var attr = new ProcThreadAttributeList(2);
                    attr.UpdatePseudoConsole(pseudoConsole.Handle.DangerousGetHandle());
                    attr.UpdateHandleList(pInheritableHandles, inheritableHandles.Length);

                    const int CreationFlags =
                        Kernel32.CREATE_UNICODE_ENVIRONMENT
                        | Kernel32.EXTENDED_STARTUPINFO_PRESENT;

                    SafeThreadHandle threadHandle;
                    (_, processHandle, threadHandle) = InvokeCreateProcess(
                        commandLine,
                        CreationFlags,
                        null,
                        workingDirectory,
                        childStdIn,
                        childStdOut,
                        childStdErr,
                        attr);
                    threadHandle.Dispose();
                }

                using var waitHandle = new WindowsProcessWaitHandle(processHandle);
                waitHandle.WaitOne();

                if (!Kernel32.GetExitCodeProcess(processHandle, out var exitCode))
                {
                    throw new Win32Exception();
                }

                if (exitCode != 0)
                {
                    ThrowHelper.ThrowChcpFailedException(codePage, exitCode, nameof(codePage));
                }
            }
            finally
            {
                processHandle?.Dispose();
            }
        }

        private static unsafe SafeJobObjectHandle CreateJobObject(
            bool killOnClose,
            bool disableWindowsErrorReportingDialog)
        {
            var jobObjectHandle = Kernel32.CreateJobObject(IntPtr.Zero, null);
            try
            {
                if (jobObjectHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }

                var limitFlags = Kernel32.JOB_OBJECT_LIMIT_BREAKAWAY_OK;
                if (disableWindowsErrorReportingDialog)
                {
                    limitFlags |= Kernel32.JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION;
                }
                if (killOnClose)
                {
                    limitFlags |= Kernel32.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
                }

                var extendedLimitInformation = default(Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
                extendedLimitInformation.BasicLimitInformation.LimitFlags = (uint)limitFlags;
                if (!Kernel32.SetInformationJobObject(
                    jobObjectHandle,
                    Kernel32.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    &extendedLimitInformation,
                    sizeof(Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
                {
                    throw new Win32Exception();
                }

                return jobObjectHandle;
            }
            catch
            {
                jobObjectHandle.Dispose();
                throw;
            }
        }

        private static unsafe (int processId, SafeProcessHandle processHandle, SafeThreadHandle threadHandle) InvokeCreateProcess(
            StringBuilder commandLine,
            int creationFlags,
            char[]? environmentBlock,
            string? workingDirectory,
            SafeHandle? childStdIn,
            SafeHandle? childStdOut,
            SafeHandle? childStdErr,
            ProcThreadAttributeList attr)
        {
            var nativeSi = new Kernel32.STARTUPINFOEX()
            {
                cb = sizeof(Kernel32.STARTUPINFOEX),
                dwFlags = Kernel32.STARTF_USESTDHANDLES,
                hStdInput = childStdIn?.DangerousGetHandle() ?? IntPtr.Zero,
                hStdOutput = childStdOut?.DangerousGetHandle() ?? IntPtr.Zero,
                hStdError = childStdErr?.DangerousGetHandle() ?? IntPtr.Zero,
                lpAttributeList = attr.DangerousGetHandle(),
            };

            fixed (char* pEnvironmentBlock = environmentBlock)
            {
                int currentRetryInterval = RetryIntervalMilliseconds;

                for (int i = 0; i < MaxAttemptCount; i++)
                {
                    if (!Kernel32.CreateProcess(
                         null,
                         commandLine,
                         IntPtr.Zero,
                         IntPtr.Zero,
                         true,
                         creationFlags,
                         pEnvironmentBlock,
                         workingDirectory,
                         ref nativeSi,
                         out var pi))
                    {
                        int errorCode = Marshal.GetLastWin32Error();

                        if (errorCode == Kernel32.ERROR_NOT_ENOUGH_MEMORY && i < MaxAttemptCount - 1)
                        {
                            // Massively concurrent invocation of CreateProcess within one process intermittently fails with ERROR_NOT_ENOUGH_MEMORY.
                            // Retry on such failure.
                            Thread.Sleep(currentRetryInterval);
                            currentRetryInterval = Math.Min(currentRetryInterval * 2, MaxRetryIntervalMilliseconds);
                            continue;
                        }

                        throw new Win32Exception();
                    }

                    return (pi.dwProcessId, new SafeProcessHandle(pi.hProcess, true), new SafeThreadHandle(pi.hThread, true));
                }

                throw new AsmichiChildProcessInternalLogicErrorException();
            }
        }
    }
}
