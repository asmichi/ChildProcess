// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class NtDll
    {
        private const string DllName = "ntdll.dll";

        [DllImport(DllName, EntryPoint = "RtlGetVersion", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int RtlGetVersion([In, Out] ref RTL_OSVERSIONINFOW lpVersionInformation);

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct RTL_OSVERSIONINFOW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            public fixed char szCSDVersion[128];
        }
    }
}
