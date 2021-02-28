// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.ComponentModel;
using System.Threading;
using Asmichi.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    internal sealed class WindowsProcessWaitHandle : WaitHandle
    {
        public WindowsProcessWaitHandle(SafeProcessHandle processHandle)
        {
            WaitHandleExtensions.SetSafeWaitHandle(this, ToSafeWaitHandle(processHandle));
        }

        private static SafeWaitHandle ToSafeWaitHandle(SafeProcessHandle handle)
        {
            if (!Kernel32.DuplicateHandle(
                Kernel32.GetCurrentProcess(),
                handle,
                Kernel32.GetCurrentProcess(),
                out SafeWaitHandle waitHandle,
                0,
                false,
                Kernel32.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception();
            }

            return waitHandle;
        }
    }
}
