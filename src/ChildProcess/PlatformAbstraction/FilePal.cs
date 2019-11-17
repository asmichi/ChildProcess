// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction
{
    internal static class FilePal
    {
        public static SafeFileHandle OpenNullDevice(FileAccess fileAccess)
        {
#if NETFRAMEWORK
            return Windows.FilePalWindows.OpenNullDevice(fileAccess);
#else
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.FilePalWindows.OpenNullDevice(fileAccess);
                case PlatformKind.Linux:
                    return Linux.FilePalLinux.OpenNullDevice(fileAccess);
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
#endif
        }

        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
#if NETFRAMEWORK
            return Windows.FilePalWindows.CreatePipePair();
#else
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.FilePalWindows.CreatePipePair();
                case PlatformKind.Linux:
                    return Linux.FilePalLinux.CreatePipePair();
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
#endif
        }

        /// <summary>
        /// Creates a pipe pair. Asynchronous IO is enabled for the server side.
        /// If <paramref name="pipeDirection"/> is <see cref="PipeDirection.In"/>, clientPipe is created with asynchronous IO enabled.
        /// If <see cref="PipeDirection.Out"/>, serverStream is created with asynchronous moIOde enabled.
        /// </summary>
        /// <param name="pipeDirection">Specifies which side is the server side.</param>
        /// <returns>A pipe pair.</returns>
        public static (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection)
        {
#if NETFRAMEWORK
            return Windows.FilePalWindows.CreatePipePairWithAsyncServerSide(pipeDirection);
#else
            switch (Pal.PlatformKind)
            {
                case PlatformKind.Win32:
                    return Windows.FilePalWindows.CreatePipePairWithAsyncServerSide(pipeDirection);
                case PlatformKind.Linux:
                    return Linux.FilePalLinux.CreatePipePairWithAsyncServerSide(pipeDirection);
                case PlatformKind.Unknown:
                default:
                    throw new PlatformNotSupportedException();
            }
#endif
        }
    }
}
