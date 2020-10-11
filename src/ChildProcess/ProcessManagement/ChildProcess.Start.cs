// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Asmichi.Utilities.PlatformAbstraction;
using Asmichi.Utilities.Utilities;

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
        /// <exception cref="ArgumentException"><paramref name="startInfo"/> has an invalid value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is null.</exception>
        /// <exception cref="FileNotFoundException">The executable not found.</exception>
        /// <exception cref="IOException">Failed to open a specified file.</exception>
        /// <exception cref="AsmichiChildProcessLibraryCrashedException">The operation failed due to critical disturbance.</exception>
        /// <exception cref="Win32Exception">Another kind of native errors.</exception>
        public static IChildProcess Start(ChildProcessStartInfo startInfo)
        {
            _ = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _ = startInfo.FileName ?? throw new ArgumentException("ChildProcessStartInfo.FileName must not be null.", nameof(startInfo));
            _ = startInfo.Arguments ?? throw new ArgumentException("ChildProcessStartInfo.Arguments must not be null.", nameof(startInfo));

            bool ignoreSearchPath = (startInfo.Flags & ChildProcessFlags.IgnoreSearchPath) != 0;
            var resolvedFileName = ResolveExecutablePath(startInfo.FileName, ignoreSearchPath);

            using var stdHandles = new PipelineStdHandleCreator(startInfo);
            IChildProcessStateHolder processState;
            try
            {
                processState = ChildProcessContext.Shared.SpawnProcess(
                    fileName: resolvedFileName,
                    arguments: startInfo.Arguments,
                    workingDirectory: startInfo.WorkingDirectory,
                    environmentVariables: startInfo.EnvironmentVariables,
                    stdIn: stdHandles.PipelineStdIn,
                    stdOut: stdHandles.PipelineStdOut,
                    stdErr: stdHandles.PipelineStdErr);
            }
            catch (Win32Exception ex)
            {
                if (EnvironmentPal.IsFileNotFoundError(ex.NativeErrorCode))
                {
                    ThrowHelper.ThrowExecutableNotFoundException(resolvedFileName, ignoreSearchPath, ex);
                }

                // Win32Exception does not provide detailed information by its type.
                // The NativeErrorCode and Message property should be enough because normally there is
                // nothing we can do to programmatically recover from this error.
                throw;
            }

            var process = new ChildProcess(processState, stdHandles.InputStream, stdHandles.OutputStream, stdHandles.ErrorStream);
            stdHandles.DetachStreams();
            return process;
        }

        private static string ResolveExecutablePath(string fileName, bool ignoreSearchPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Append ".exe"
                if (!Path.GetFileName(fileName.AsSpan()).Contains('.'))
                {
                    fileName += ".exe";
                }
            }

            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }

            string? resolvedPath;
            if (TryResolveRelativeExecutablePath(fileName, Environment.CurrentDirectory, out resolvedPath))
            {
                return resolvedPath;
            }

            if (!ignoreSearchPath)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (TryResolveRelativeExecutablePathBySpecialFolder(fileName, Environment.SpecialFolder.System, out resolvedPath))
                    {
                        return resolvedPath;
                    }
                    if (TryResolveRelativeExecutablePathBySpecialFolder(fileName, Environment.SpecialFolder.Windows, out resolvedPath))
                    {
                        return resolvedPath;
                    }
                }

                var searchPath = Environment.GetEnvironmentVariable("PATH");
                if (searchPath is { })
                {
                    var remainingSearchPath = searchPath.AsSpan();
                    while (true)
                    {
                        var separatorIndex = remainingSearchPath.IndexOf(EnvironmentPal.SearchPathSeparator);
                        var dir = separatorIndex < 0 ? remainingSearchPath : remainingSearchPath.Slice(0, separatorIndex);
                        if (dir.Length > 0 && TryResolveRelativeExecutablePath(fileName, dir, out resolvedPath))
                        {
                            return resolvedPath;
                        }
                        if (separatorIndex < 0)
                        {
                            break;
                        }
                        remainingSearchPath = remainingSearchPath.Slice(separatorIndex + 1);
                    }
                }
            }

            ThrowHelper.ThrowExecutableNotFoundException(fileName, ignoreSearchPath);
            return null;
        }

        private static bool TryResolveRelativeExecutablePathBySpecialFolder(string fileName, Environment.SpecialFolder folder, [NotNullWhen(true)] out string? resolvedPath) =>
            TryResolveRelativeExecutablePath(
                fileName,
                Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify),
                out resolvedPath);

        private static bool TryResolveRelativeExecutablePath(string fileName, ReadOnlySpan<char> baseDir, [NotNullWhen(true)] out string? resolvedPath)
        {
            Debug.Assert(!Path.IsPathRooted(fileName));

            var candidate = Path.Join(baseDir, fileName.AsSpan());
            if (File.Exists(candidate))
            {
                resolvedPath = candidate;
                return true;
            }
            else
            {
                resolvedPath = null;
                return false;
            }
        }
    }
}
