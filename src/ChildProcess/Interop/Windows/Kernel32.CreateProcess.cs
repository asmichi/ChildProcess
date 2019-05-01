// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
#pragma warning disable SA1310 // Field names must not contain underscore

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class Kernel32
    {
        public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const int EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        public const int STARTF_USESTDHANDLES = 0x00000100;
        public const int CREATE_NO_WINDOW = 0x08000000;

        [DllImport(DllName, EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern unsafe bool CreateProcess(
            [In] string lpApplicationName,
            [In] StringBuilder lpCommandLine,
            [In] IntPtr procSecAttrs,
            [In] IntPtr threadSecAttrs,
            [In] bool bInheritHandles,
            [In] int dwCreationFlags,
            [In] char* lpEnvironment,
            [In] string lpCurrentDirectory,
            [In][Out] ref STARTUPINFOEX lpStartupInfo,
            [Out] out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOEX
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
            public IntPtr lpAttributeList;
        }
    }
}
