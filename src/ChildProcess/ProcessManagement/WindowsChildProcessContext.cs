// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.PlatformAbstraction;
using Asmichi.Utilities.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    internal sealed class WindowsChildProcessContext : IChildProcessContext
    {
        private static readonly string ChcpPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.System, Environment.SpecialFolderOption.DoNotVerify),
            "chcp.com");

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

            var pseudoConsole = InputWriterOnlyPseudoConsole.Create();

            try
            {
                if (startInfo.Flags.HasFlag(ChildProcessFlags.UseCustomCodePage))
                {
                    ChangeCodePage(pseudoConsole, startInfo.CodePage, workingDirectory);
                }

                using var inheritableHandleStore = new InheritableHandleStore(3);
                var childStdIn = stdIn != null ? inheritableHandleStore.Add(stdIn) : null;
                var childStdOut = stdOut != null ? inheritableHandleStore.Add(stdOut) : null;
                var childStdErr = stdErr != null ? inheritableHandleStore.Add(stdErr) : null;

                Span<IntPtr> inheritableHandles = stackalloc IntPtr[inheritableHandleStore.Count];
                inheritableHandleStore.DangerousGetHandles(inheritableHandles);
                fixed (IntPtr* pInheritableHandles = inheritableHandles)
                {
                    using var attr = new ProcThreadAttributeList(2);
                    attr.UpdatePseudoConsole(pseudoConsole.Handle.DangerousGetHandle());
                    attr.UpdateHandleList(pInheritableHandles, inheritableHandles.Length);

                    // Support for attached child processes temporarily removed.
                    int creationFlags = Kernel32.CREATE_UNICODE_ENVIRONMENT | Kernel32.EXTENDED_STARTUPINFO_PRESENT;

                    var processHandle = InvokeCreateProcess(
                        commandLine,
                        creationFlags,
                        environmentBlock,
                        workingDirectory,
                        childStdIn,
                        childStdOut,
                        childStdErr,
                        attr);

                    return new WindowsChildProcessState(processHandle, pseudoConsole);
                }
            }
            catch
            {
                pseudoConsole.Dispose();
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
            WindowsCommandLineUtil.AppendArgumentQuoted(commandLine, ChcpPath);
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

                    const int creationFlags = Kernel32.CREATE_UNICODE_ENVIRONMENT | Kernel32.EXTENDED_STARTUPINFO_PRESENT;

                    processHandle = InvokeCreateProcess(
                        commandLine,
                        creationFlags,
                        null,
                        workingDirectory,
                        childStdIn,
                        childStdOut,
                        childStdErr,
                        attr);
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
