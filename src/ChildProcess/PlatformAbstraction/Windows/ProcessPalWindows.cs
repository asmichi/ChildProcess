// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.ProcessManagement;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal static class ProcessPalWindows
    {
        internal static unsafe SafeProcessHandle SpawnProcess(
            string fileName,
            IReadOnlyCollection<string> arguments,
            string workingDirectory,
            IReadOnlyCollection<(string name, string value)> environmentVariables,
            SafeHandle stdIn,
            SafeHandle stdOut,
            SafeHandle stdErr)
        {
            var commandLine = CommandLineUtil.MakeCommandLine(fileName, arguments ?? Array.Empty<string>());
            var environmentBlock = environmentVariables != null ? EnvironmentBlockUtil.MakeEnvironmentBlockWin32(environmentVariables) : null;

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

                try
                {
                    return InvokeCreateProcess(
                        commandLine,
                        creationFlags,
                        environmentBlock,
                        workingDirectory,
                        childStdInput,
                        childStdOutput,
                        childStdError,
                        attr);
                }
                catch (Win32Exception e)
                {
                    throw new ProcessCreationFailedException(
                        string.Format(CultureInfo.CurrentCulture, "Process cannot be created: {0}", e.Message));
                }
            }
        }

        private static unsafe SafeProcessHandle InvokeCreateProcess(
            StringBuilder commandLine,
            int creationFlags,
            char[] environmentBlock,
            string currentDirectory,
            SafeHandle stdInput,
            SafeHandle stdOutput,
            SafeHandle stdError,
            ProcThreadAttributeList attr)
        {
            commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
            attr = attr ?? throw new ArgumentNullException(nameof(attr));

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
                var pi = default(Kernel32.PROCESS_INFORMATION);
                bool processCreated = false;
                var process = default(SafeProcessHandle);

                bool stdInputRefAdded = false;
                bool stdOutputRefAdded = false;
                bool stdErrorRefAdded = false;
                bool attrRefAdded = false;
                int win32Error = 0;

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    stdInput?.DangerousAddRef(ref stdInputRefAdded);
                    stdOutput?.DangerousAddRef(ref stdOutputRefAdded);
                    stdError?.DangerousAddRef(ref stdErrorRefAdded);
                    attr.DangerousAddRef(ref attrRefAdded);

                    // Ensure handles are either closed or wrapped in SafeHandle even when the try block is interrupted.
                    processCreated = Kernel32.CreateProcess(
                        null,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        true,
                        creationFlags,
                        pEnvironment,
                        currentDirectory,
                        ref nativeSi,
                        out pi);

                    if (processCreated)
                    {
                        Kernel32.CloseHandle(pi.hThread);
                        process = new SafeProcessHandle(pi.hProcess, true);
                    }
                    else
                    {
                        win32Error = Marshal.GetLastWin32Error();
                    }

                    if (stdInputRefAdded)
                    {
                        stdInput.DangerousRelease();
                    }
                    if (stdOutputRefAdded)
                    {
                        stdOutput.DangerousRelease();
                    }
                    if (stdErrorRefAdded)
                    {
                        stdError.DangerousRelease();
                    }
                    if (attrRefAdded)
                    {
                        attr.DangerousRelease();
                    }
                }

                if (!processCreated)
                {
                    // Win32Exception does not provide detailed information by its type.
                    // The NativeErrorCode and Message property should be enough because normally there is
                    // nothing we can do to programmatically recover from this error.
                    throw new Win32Exception(win32Error);
                }

                return process;
            }
        }
    }
}
