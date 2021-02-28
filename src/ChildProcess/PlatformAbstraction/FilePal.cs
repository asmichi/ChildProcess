// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.PlatformAbstraction
{
    internal interface IFilePal
    {
        (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair();
        (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection);
        SafeFileHandle OpenNullDevice(FileAccess fileAccess);
    }

    internal static class FilePal
    {
        private static readonly IFilePal Impl = CreatePlatformSpecificImpl();

        private static IFilePal CreatePlatformSpecificImpl()
        {
            return Pal.PlatformKind switch
            {
                PlatformKind.Win32 => new Windows.WindowsFilePal(),
                PlatformKind.Linux => new Unix.UnixFilePal(),
                PlatformKind.Unknown => throw new PlatformNotSupportedException(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static SafeFileHandle OpenNullDevice(FileAccess fileAccess) => Impl.OpenNullDevice(fileAccess);

        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair() => Impl.CreatePipePair();

        /// <summary>
        /// Creates a pipe pair. Asynchronous IO is enabled for the server side.
        /// If <paramref name="pipeDirection"/> is <see cref="PipeDirection.In"/>, clientPipe is created with asynchronous IO enabled.
        /// If <see cref="PipeDirection.Out"/>, serverStream is created with asynchronous moIOde enabled.
        /// </summary>
        /// <param name="pipeDirection">Specifies which side is the server side.</param>
        /// <returns>A pipe pair.</returns>
        public static (Stream serverStream, SafeFileHandle clientPipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection) =>
            Impl.CreatePipePairWithAsyncServerSide(pipeDirection);
    }
}
