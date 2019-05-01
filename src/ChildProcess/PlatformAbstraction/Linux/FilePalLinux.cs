// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Linux
{
    internal static class FilePalLinux
    {
        public static SafeFileHandle OpenNullDevice(FileAccess fileAccess)
        {
            throw new NotImplementedException();
        }

        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
            throw new NotImplementedException();
        }

        public static (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection)
        {
            throw new NotImplementedException();
        }
    }
}
