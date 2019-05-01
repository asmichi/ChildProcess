// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Linux
{
    internal static class HandlePalLinux
    {
        public static SafeWaitHandle ToWaitHandle(SafeProcessHandle handle)
        {
            throw new NotImplementedException();
        }
    }
}
