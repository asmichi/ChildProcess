// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using Asmichi.PlatformAbstraction;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Interop.Windows
{
    internal sealed class InputWriterOnlyPseudoConsole : IDisposable
    {
        private readonly SafePseudoConsoleHandle _handle;
        private readonly SafeFileHandle _consoleInputWriter;

        private InputWriterOnlyPseudoConsole(SafePseudoConsoleHandle handle, SafeFileHandle consoleInputWriter)
        {
            _handle = handle;
            _consoleInputWriter = consoleInputWriter;
        }

        public void Dispose()
        {
            _handle.Dispose();
            _consoleInputWriter.Dispose();
        }

        public SafePseudoConsoleHandle Handle => _handle;
        public SafeFileHandle ConsoleInputWriter => _consoleInputWriter;

        public static InputWriterOnlyPseudoConsole Create()
        {
            var (inputReader, inputWriter) = FilePal.CreatePipePair();
            try
            {
                using var outputWriter = FilePal.OpenNullDevice(FileAccess.Write);
                var hPC = SafePseudoConsoleHandle.Create(inputReader, outputWriter);
                return new InputWriterOnlyPseudoConsole(hPC, inputWriter);
            }
            catch
            {
                inputWriter.Dispose();
                throw;
            }
            finally
            {
                inputReader.Dispose();
            }
        }
    }
}
