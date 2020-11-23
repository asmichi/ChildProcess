// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.PlatformAbstraction;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class WindowsChildProcessContext : IChildProcessContext
    {
        public unsafe IChildProcessStateHolder SpawnProcess(
            string fileName,
            IReadOnlyCollection<string> arguments,
            string? workingDirectory,
            IReadOnlyCollection<KeyValuePair<string, string>>? environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            var commandLine = WindowsCommandLineUtil.MakeCommandLine(fileName, arguments ?? Array.Empty<string>());
            var environmentBlock = environmentVariables != null ? WindowsEnvironmentBlockUtil.MakeEnvironmentBlock(environmentVariables) : null;

            using var inheritableHandleStore = new InheritableHandleStore(3);
            var childStdInput = stdIn != null ? inheritableHandleStore.Add(stdIn) : null;
            var childStdOutput = stdOut != null ? inheritableHandleStore.Add(stdOut) : null;
            var childStdError = stdErr != null ? inheritableHandleStore.Add(stdErr) : null;

            Span<IntPtr> inheritableHandles = stackalloc IntPtr[inheritableHandleStore.Count];
            inheritableHandleStore.DangerousGetHandles(inheritableHandles);

            fixed (IntPtr* pInheritableHandles = inheritableHandles)
            {
                using var attr = new ProcThreadAttributeList(1);
                attr.UpdateHandleList(pInheritableHandles, inheritableHandles.Length);

                bool createNoWindow = !ConsolePal.HasConsoleWindow();
                int creationFlags =
                    Kernel32.CREATE_UNICODE_ENVIRONMENT
                    | Kernel32.EXTENDED_STARTUPINFO_PRESENT
                    | (createNoWindow ? Kernel32.CREATE_NO_WINDOW : 0);

                var processHandle = InvokeCreateProcess(
                    commandLine,
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    childStdInput,
                    childStdOutput,
                    childStdError,
                    attr);
                return new WindowsChildProcessState(processHandle);
            }
        }

        private static unsafe SafeProcessHandle InvokeCreateProcess(
            StringBuilder commandLine,
            int creationFlags,
            char[]? environmentBlock,
            string? currentDirectory,
            SafeHandle? stdInput,
            SafeHandle? stdOutput,
            SafeHandle? stdError,
            ProcThreadAttributeList attr)
        {
            _ = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
            _ = attr ?? throw new ArgumentNullException(nameof(attr));

            var nativeSi = new Kernel32.STARTUPINFOEX()
            {
                cb = sizeof(Kernel32.STARTUPINFOEX),
                dwFlags = Kernel32.STARTF_USESTDHANDLES,
                hStdInput = stdInput?.DangerousGetHandle() ?? IntPtr.Zero,
                hStdOutput = stdOutput?.DangerousGetHandle() ?? IntPtr.Zero,
                hStdError = stdError?.DangerousGetHandle() ?? IntPtr.Zero,
                lpAttributeList = attr.DangerousGetHandle(),
            };

            fixed (char* pEnvironment = environmentBlock)
            {
                bool stdInputRefAdded = false;
                bool stdOutputRefAdded = false;
                bool stdErrorRefAdded = false;
                bool attrRefAdded = false;

                try
                {
                    stdInput?.DangerousAddRef(ref stdInputRefAdded);
                    stdOutput?.DangerousAddRef(ref stdOutputRefAdded);
                    stdError?.DangerousAddRef(ref stdErrorRefAdded);
                    attr.DangerousAddRef(ref attrRefAdded);

                    if (!Kernel32.CreateProcess(
                        null,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        true,
                        creationFlags,
                        pEnvironment,
                        currentDirectory,
                        ref nativeSi,
                        out var pi))
                    {
                        throw new Win32Exception();
                    }

                    Kernel32.CloseHandle(pi.hThread);
                    return new SafeProcessHandle(pi.hProcess, true);
                }
                finally
                {
                    if (stdInputRefAdded)
                    {
                        stdInput!.DangerousRelease();
                    }
                    if (stdOutputRefAdded)
                    {
                        stdOutput!.DangerousRelease();
                    }
                    if (stdErrorRefAdded)
                    {
                        stdError!.DangerousRelease();
                    }
                    if (attrRefAdded)
                    {
                        attr.DangerousRelease();
                    }
                }
            }
        }
    }
}
