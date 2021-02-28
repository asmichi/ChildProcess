// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Asmichi.PlatformAbstraction;
using Asmichi.Utilities;

namespace Asmichi.ProcessManagement
{
    /// <summary>
    /// Provides functionality for creating child processes.
    /// </summary>
    public static class ChildProcess
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

            var startInfoInternal = new ChildProcessStartInfoInternal(startInfo);
            _ = startInfoInternal.FileName ?? throw new ArgumentException("ChildProcessStartInfo.FileName must not be null.", nameof(startInfo));
            _ = startInfoInternal.Arguments ?? throw new ArgumentException("ChildProcessStartInfo.Arguments must not be null.", nameof(startInfo));

            var flags = startInfoInternal.Flags;
            if (flags.HasUseCustomCodePage() && flags.HasAttachToCurrentConsole())
            {
                throw new ArgumentException(
                    $"{nameof(ChildProcessFlags.UseCustomCodePage)} cannot be combined with {nameof(ChildProcessFlags.AttachToCurrentConsole)}.", nameof(startInfo));
            }

            var resolvedPath = ResolveExecutablePath(startInfoInternal.FileName, startInfoInternal.Flags);

            using var stdHandles = new PipelineStdHandleCreator(ref startInfoInternal);
            IChildProcessStateHolder processState;
            try
            {
                processState = ChildProcessContext.Shared.SpawnProcess(
                    startInfo: ref startInfoInternal,
                    resolvedPath: resolvedPath,
                    stdIn: stdHandles.PipelineStdIn,
                    stdOut: stdHandles.PipelineStdOut,
                    stdErr: stdHandles.PipelineStdErr);
            }
            catch (Win32Exception ex)
            {
                if (EnvironmentPal.IsFileNotFoundError(ex.NativeErrorCode))
                {
                    ThrowHelper.ThrowExecutableNotFoundException(resolvedPath, startInfoInternal.Flags, ex);
                }

                // Win32Exception does not provide detailed information by its type.
                // The NativeErrorCode and Message property should be enough because normally there is
                // nothing we can do to programmatically recover from this error.
                throw;
            }

            var process = new ChildProcessImpl(processState, stdHandles.InputStream, stdHandles.OutputStream, stdHandles.ErrorStream);
            stdHandles.DetachStreams();
            return process;
        }

        private static string ResolveExecutablePath(string fileName, ChildProcessFlags flags)
        {
            bool ignoreSearchPath = flags.HasIgnoreSearchPath();
            var searchPath = ignoreSearchPath ? null : EnvironmentSearchPathCache.ResolveSearchPath();
            var resolvedPath = SearchPathSearcher.FindExecutable(fileName, flags.HasAllowRelativeFileName(), searchPath);
            if (resolvedPath is null)
            {
                ThrowHelper.ThrowExecutableNotFoundException(fileName, flags);
            }
            return resolvedPath;
        }

        private static class EnvironmentSearchPathCache
        {
            private static readonly object Lock = new object();
            private static string? _previousEnvStr = Environment.GetEnvironmentVariable("PATH");
            private static IReadOnlyList<string> _cachedSearchPath = SearchPathSearcher.ResolveSearchPath(_previousEnvStr);

            public static IReadOnlyList<string> ResolveSearchPath()
            {
                var envStr = Environment.GetEnvironmentVariable("PATH");
                lock (Lock)
                {
                    if (envStr == _previousEnvStr)
                    {
                        return _cachedSearchPath;
                    }
                    else
                    {
                        var searchPath = SearchPathSearcher.ResolveSearchPath(envStr);
                        _previousEnvStr = envStr;
                        _cachedSearchPath = searchPath;
                        return searchPath;
                    }
                }
            }
        }
    }
}
