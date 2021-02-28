// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable SA1310 // Field names must not contain underscore

namespace Asmichi.Interop.Windows
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/en-us/windows/win32/procthread/job-objects
        // JOBOBJECT_BASIC_LIMIT_INFORMATION.LimitFlags
        public const int JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800;
        public const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        [DllImport(DllName, EntryPoint = "CreateJobObjectW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeJobObjectHandle CreateJobObject(
             [In] IntPtr lpJobAttributes,
             [In] char[]? lpName);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool AssignProcessToJobObject(
            [In] SafeJobObjectHandle hJob,
            [In] SafeProcessHandle hProcess);

        [DllImport(DllName, SetLastError = true)]
        public static extern unsafe bool SetInformationJobObject(
            [In] SafeJobObjectHandle hJob,
            [In] JOBOBJECTINFOCLASS jobObjectInformationClass,
            [In] void* lpJobObjectInformation,
            [In] int cbJobObjectInformationLength);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool TerminateJobObject(
            [In] SafeJobObjectHandle hJob,
            [In] int uExitCode);

        public enum JOBOBJECTINFOCLASS : int
        {
            JobObjectBasicLimitInformation = 2,
            JobObjectExtendedLimitInformation = 9,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public nuint MinimumWorkingSetSize;
            public nuint MaximumWorkingSetSize;
            public ulong ActiveProcessLimit;
            public nuint Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public nuint ProcessMemoryLimit;
            public nuint JobMemoryLimit;
            public nuint PeakProcessMemoryUsed;
            public nuint PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }
    }
}
