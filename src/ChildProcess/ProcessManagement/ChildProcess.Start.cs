// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.PlatformAbstraction;
using Asmichi.Utilities.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    // Process creation part
    public sealed partial class ChildProcess : IDisposable
    {
        /// <summary>
        /// Starts a child process as specified in <paramref name="startInfo"/>.
        /// </summary>
        /// <param name="startInfo"><see cref="ChildProcessStartInfo"/>.</param>
        /// <returns>The started process.</returns>
        /// <exception cref="IOException">Failed to open a specified file.</exception>
        /// <exception cref="ProcessCreationFailedException">Process creation failed.</exception>
        public static ChildProcess Start(ChildProcessStartInfo startInfo)
        {
            _ = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _ = startInfo.FileName ?? throw new ArgumentException("ChildProcessStartInfo.FileName must not be null.", nameof(startInfo));
            _ = startInfo.Arguments ?? throw new ArgumentException("ChildProcessStartInfo.Arguments must not be null.", nameof(startInfo));

            using var stdHandles = new PipelineStdHandleCreator(startInfo);
            var processHandle = ProcessPal.SpawnProcess(
                fileName: startInfo.FileName,
                arguments: startInfo.Arguments,
                workingDirectory: startInfo.WorkingDirectory,
                environmentVariables: startInfo.EnvironmentVariables,
                stdIn: stdHandles.PipelineStdIn,
                stdOut: stdHandles.PipelineStdOut,
                stdErr: stdHandles.PipelineStdErr);

            try
            {
                var process = new ChildProcess(processHandle, stdHandles.InputStream, stdHandles.OutputStream, stdHandles.ErrorStream);
                stdHandles.DetachStreams();
                return process;
            }
            catch
            {
                processHandle.Dispose();
                throw;
            }
        }
    }
}
