// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            ChildProcessStartInfo startInfo,
            string resolvedPath,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            var arguments = startInfo.Arguments;
            var environmentVariables = startInfo.EnvironmentVariables;
            var workingDirectory = startInfo.WorkingDirectory;
            var commandLine = WindowsCommandLineUtil.MakeCommandLine(resolvedPath, arguments ?? Array.Empty<string>());
            var environmentBlock = environmentVariables != null ? WindowsEnvironmentBlockUtil.MakeEnvironmentBlock(environmentVariables) : null;

            using var attr = new ProcThreadAttributeList(1);

            using var inheritableHandleStore = new InheritableHandleStore(3);
            var childStdIn = stdIn != null ? inheritableHandleStore.Add(stdIn) : null;
            var childStdOut = stdOut != null ? inheritableHandleStore.Add(stdOut) : null;
            var childStdErr = stdErr != null ? inheritableHandleStore.Add(stdErr) : null;

            Span<IntPtr> inheritableHandles = stackalloc IntPtr[inheritableHandleStore.Count];
            inheritableHandleStore.DangerousGetHandles(inheritableHandles);
            fixed (IntPtr* pInheritableHandles = inheritableHandles)
            {
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
                    childStdIn,
                    childStdOut,
                    childStdErr,
                    attr);

                return new WindowsChildProcessState(processHandle);
            }
        }

        private static unsafe SafeProcessHandle InvokeCreateProcess(
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
                    throw new Win32Exception();
                }

                Kernel32.CloseHandle(pi.hThread);
                return new SafeProcessHandle(pi.hProcess, true);
            }
        }
    }
}
