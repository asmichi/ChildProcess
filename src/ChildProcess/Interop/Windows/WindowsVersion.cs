// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.Utilities.ProcessManagement;
using static System.FormattableString;

namespace Asmichi.Utilities.Interop.Windows
{
    internal static class WindowsVersion
    {
        /// <summary>
        /// On Windows 10 1809 (including Windows Server 2019), <see cref="WindowsChildProcessState.SignalTermination"/>
        /// will just perform <see cref="WindowsChildProcessState.Kill"/>.
        /// This is due to a Windows pseudoconsole bug where ClosePseudoConsole does not terminate
        /// applications attached to the pseudoconsole.
        /// </summary>
        public static bool NeedsWorkaroundForWindows1809 { get; } = GetIsWindows1809();

        private static unsafe bool GetIsWindows1809()
        {
            // Resort to ntdll. OsGetVersionEx and hence Environment.OSVersion.Version (till .NET Core 3.1)
            // will always return Windows 8.1 if the app is not manifested to support newer versions.
            // https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getversionexw
            // https://github.com/dotnet/runtime/pull/33651
            var osvi = default(NtDll.RTL_OSVERSIONINFOW);
            osvi.dwOSVersionInfoSize = (uint)sizeof(NtDll.RTL_OSVERSIONINFOW);
            int ntstatus = NtDll.RtlGetVersion(ref osvi);
            if (ntstatus < 0)
            {
                throw new AsmichiChildProcessInternalLogicErrorException(Invariant($"RtlGetVersion failed (0x{ntstatus:X})."));
            }

            return osvi.dwPlatformId == 2
                && osvi.dwMajorVersion == 10
                && osvi.dwMinorVersion == 0
                && osvi.dwBuildNumber == 17763;
        }
    }
}
