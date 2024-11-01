// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Asmichi.PlatformAbstraction;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Provides in/out/err handles of a pipeline.
    /// </summary>
    internal sealed class PipelineStdHandleCreator : IDisposable
    {
        private readonly SafeFileHandle? _inputReadPipe;
        private readonly SafeFileHandle? _outputWritePipe;
        private readonly SafeFileHandle? _errorWritePipe;
        private List<IDisposable>? _objectsToDispose;
        private bool _isDisposed;

        public PipelineStdHandleCreator(ref ChildProcessStartInfoInternal startInfo)
        {
            var stdInputRedirection = startInfo.StdInputRedirection;
            var stdOutputRedirection = startInfo.StdOutputRedirection;
            var stdErrorRedirection = startInfo.StdErrorRedirection;
            var stdInputFile = startInfo.StdInputFile;
            var stdOutputFile = startInfo.StdOutputFile;
            var stdErrorFile = startInfo.StdErrorFile;
            var stdInputHandle = startInfo.StdInputHandle;
            var stdOutputHandle = startInfo.StdOutputHandle;
            var stdErrorHandle = startInfo.StdErrorHandle;

            if (stdInputRedirection == InputRedirection.Handle && stdInputHandle == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdInputHandle)} must not be null.", nameof(startInfo));
            }
            if (stdInputRedirection == InputRedirection.File && stdInputFile == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdInputFile)} must not be null.", nameof(startInfo));
            }
            if (stdOutputRedirection == OutputRedirection.Handle && stdOutputHandle == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdOutputHandle)} must not be null.", nameof(startInfo));
            }
            if (IsFileRedirection(stdOutputRedirection) && stdOutputFile == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdOutputFile)} must not be null.", nameof(startInfo));
            }
            if (stdErrorRedirection == OutputRedirection.Handle && stdErrorHandle == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdErrorHandle)} must not be null.", nameof(startInfo));
            }
            if (IsFileRedirection(stdErrorRedirection) && stdErrorFile == null)
            {
                throw new ArgumentException($"{nameof(ChildProcessStartInfo.StdErrorFile)} must not be null.", nameof(startInfo));
            }

            bool redirectingToSameFile = IsFileRedirection(stdOutputRedirection) && IsFileRedirection(stdErrorRedirection) && stdOutputFile == stdErrorFile;
            if (redirectingToSameFile && stdErrorRedirection != stdOutputRedirection)
            {
                throw new ArgumentException(
                    "StdOutputRedirection and StdErrorRedirection must be the same value when both stdout and stderr redirect to the same file.",
                    nameof(startInfo));
            }

            try
            {
                if (stdInputRedirection == InputRedirection.InputPipe)
                {
                    (InputStream, _inputReadPipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.Out);
                }

                if (stdOutputRedirection == OutputRedirection.OutputPipe
                    || stdErrorRedirection == OutputRedirection.OutputPipe)
                {
                    (OutputStream, _outputWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.In);
                }

                if (stdOutputRedirection == OutputRedirection.ErrorPipe
                    || stdErrorRedirection == OutputRedirection.ErrorPipe)
                {
                    (ErrorStream, _errorWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.In);
                }

                PipelineStdIn = ChooseInput(
                    stdInputRedirection,
                    stdInputFile,
                    stdInputHandle,
                    _inputReadPipe,
                    startInfo.CreateNewConsole);

                PipelineStdOut = ChooseOutput(
                    stdOutputRedirection,
                    stdOutputFile,
                    stdOutputHandle,
                    _outputWritePipe,
                    _errorWritePipe,
                    startInfo.CreateNewConsole);

                if (redirectingToSameFile)
                {
                    PipelineStdErr = PipelineStdOut;
                }
                else
                {
                    PipelineStdErr = ChooseOutput(
                        stdErrorRedirection,
                        stdErrorFile,
                        stdErrorHandle,
                        _outputWritePipe,
                        _errorWritePipe,
                        startInfo.CreateNewConsole);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_objectsToDispose != null)
                {
                    foreach (var h in _objectsToDispose)
                    {
                        h.Dispose();
                    }
                }

                _inputReadPipe?.Dispose();
                _outputWritePipe?.Dispose();
                _errorWritePipe?.Dispose();
                InputStream?.Dispose();
                OutputStream?.Dispose();
                ErrorStream?.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// A handle that should be used as the stdin handle of the pipeline.
        /// </summary>
        public SafeHandle PipelineStdIn { get; }

        /// <summary>
        /// A handle that should be used as the stdout handle of the pipeline.
        /// </summary>
        public SafeHandle PipelineStdOut { get; }

        /// <summary>
        /// A handle that should be used as the stderr handle of the pipeline.
        /// </summary>
        public SafeHandle PipelineStdErr { get; }

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that writes to the pipeline.
        /// </summary>
        public Stream? InputStream { get; private set; }

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that reads from the standard output of the pipeline.
        /// </summary>
        public Stream? OutputStream { get; private set; }

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that reads from the standard error of the pipeline.
        /// </summary>
        public Stream? ErrorStream { get; private set; }

        /// <summary>
        /// Detaches <see cref="InputStream"/>, <see cref="OutputStream"/> and <see cref="ErrorStream"/> so that they will no be disposed by this instance.
        /// Must be called in order to expose the streams to the caller.
        /// </summary>
        public void DetachStreams()
        {
            InputStream = null;
            OutputStream = null;
            ErrorStream = null;
        }

        private SafeHandle ChooseInput(
            InputRedirection redirection,
            string? fileName,
            SafeHandle? handle,
            SafeHandle? inputPipe,
            bool createNewConsole)
        {
            return redirection switch
            {
                InputRedirection.ParentInput => CreateStdInputHandleForChild(createNewConsole) ?? OpenNullDevice(FileAccess.Read),
                InputRedirection.InputPipe => inputPipe!,
                InputRedirection.File => OpenFile(fileName!, FileMode.Open, FileAccess.Read, FileShare.Read),
                InputRedirection.Handle => handle!,
                InputRedirection.NullDevice => OpenNullDevice(FileAccess.Read),
                _ => throw new ArgumentOutOfRangeException(nameof(redirection), "Not a valid value for " + nameof(InputRedirection) + "."),
            };
        }

        private SafeHandle ChooseOutput(
            OutputRedirection redirection,
            string? fileName,
            SafeHandle? handle,
            SafeHandle? outputPipe,
            SafeHandle? errorPipe,
            bool createNewConsole)
        {
            return redirection switch
            {
                OutputRedirection.ParentOutput => CreateStdOutputHandleForChild(createNewConsole) ?? OpenNullDevice(FileAccess.Write),
                OutputRedirection.ParentError => CreateStdErrorHandleForChild(createNewConsole) ?? OpenNullDevice(FileAccess.Write),
                OutputRedirection.OutputPipe => outputPipe!,
                OutputRedirection.ErrorPipe => errorPipe!,
                OutputRedirection.File => OpenFile(fileName!, FileMode.Create, FileAccess.Write, FileShare.Read),
                OutputRedirection.AppendToFile => OpenFile(fileName!, FileMode.Append, FileAccess.Write, FileShare.Read),
                OutputRedirection.Handle => handle!,
                OutputRedirection.NullDevice => OpenNullDevice(FileAccess.Write),
                _ => throw new ArgumentOutOfRangeException(nameof(redirection), "Not a valid value for " + nameof(OutputRedirection) + "."),
            };
        }

        private SafeFileHandle? CreateStdInputHandleForChild(bool createNewConsole)
        {
            var handle = ConsolePal.CreateStdInputHandleForChild(createNewConsole);
            if (handle is null)
            {
                return null;
            }
            AddObjectsToDispose(handle);
            return handle;
        }

        private SafeFileHandle? CreateStdOutputHandleForChild(bool createNewConsole)
        {
            var handle = ConsolePal.CreateStdOutputHandleForChild(createNewConsole);
            if (handle is null)
            {
                return null;
            }
            AddObjectsToDispose(handle);
            return handle;
        }

        private SafeFileHandle? CreateStdErrorHandleForChild(bool createNewConsole)
        {
            var handle = ConsolePal.CreateStdErrorHandleForChild(createNewConsole);
            if (handle is null)
            {
                return null;
            }
            AddObjectsToDispose(handle);
            return handle;
        }

        private SafeFileHandle OpenFile(
            string fileName,
            FileMode mode,
            FileAccess access,
            FileShare share)
        {
            var fs = new FileStream(fileName, mode, access, share);
            AddObjectsToDispose(fs);
            return fs.SafeFileHandle;
        }

        private SafeFileHandle OpenNullDevice(FileAccess access)
        {
            var handle = FilePal.OpenNullDevice(access);
            AddObjectsToDispose(handle);
            return handle;
        }

        private void AddObjectsToDispose(IDisposable value)
        {
            if (_objectsToDispose == null)
            {
                _objectsToDispose = new List<IDisposable>(5);
            }

            _objectsToDispose.Add(value);
        }

        private static bool IsFileRedirection(OutputRedirection redirection) =>
            redirection == OutputRedirection.File || redirection == OutputRedirection.AppendToFile;
    }
}
