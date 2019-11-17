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
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.FilePalWindows.OpenNullDevice(fileAccess),
                PlatformKind.Linux => Linux.FilePalLinux.OpenNullDevice(fileAccess),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
#endif
        }

        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
#if NETFRAMEWORK
            return Windows.FilePalWindows.CreatePipePair();
#else
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.FilePalWindows.CreatePipePair(),
                PlatformKind.Linux => Linux.FilePalLinux.CreatePipePair(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
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
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => Windows.FilePalWindows.CreatePipePairWithAsyncServerSide(pipeDirection),
                PlatformKind.Linux => Linux.FilePalLinux.CreatePipePairWithAsyncServerSide(pipeDirection),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
#endif
        }
    }
}
