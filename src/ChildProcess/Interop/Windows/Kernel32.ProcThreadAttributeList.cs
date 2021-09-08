// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;

#pragma warning disable SA1310 // Field names must not contain underscore
#pragma warning disable SA1313 // Parameter names must begin with lower-case letter

namespace Asmichi.Interop.Windows
{
    internal static partial class Kernel32
    {
        public static readonly IntPtr PROC_THREAD_ATTRIBUTE_HANDLE_LIST = new IntPtr(0x20002);
        public static readonly IntPtr PROC_THREAD_ATTRIBUTE_JOB_LIST = new IntPtr(0x2000d);
        public static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = new IntPtr(0x20016);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool InitializeProcThreadAttributeList(
            [In] IntPtr lpAttributeList,
            [In] int dwAttributeCount,
            [In] int dwFlags,
            [In][Out] ref nint lpSize);

        [DllImport(DllName, SetLastError = true)]
        public static extern void DeleteProcThreadAttributeList(
            [In] IntPtr lpAttributeList);

        [DllImport(DllName, SetLastError = true)]
        public static extern unsafe bool UpdateProcThreadAttribute(
            [In] SafeUnmanagedProcThreadAttributeList lpAttributeList,
            [In] int dwFlags,
            [In] IntPtr Attribute,
            [In] void* lpValue,
            [In] nint cbSize,
            [In] IntPtr lpPreviousValue,
            [In] IntPtr lpReturnSize);
    }
}
