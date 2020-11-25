// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Asmichi.Utilities.Utilities
{
    /// <summary>
    /// Searches a search path for a file.
    /// </summary>
    internal static class SearchPathSearcher
    {
        public static string? FindExecutable(string fileName, bool searchCurrentDirectory, IReadOnlyList<string>? searchPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Append ".exe"
                if (!Path.GetFileName(fileName.AsSpan()).Contains('.'))
                {
                    fileName += ".exe";
                }
            }

            return FindFile(fileName, searchCurrentDirectory, searchPath);
        }

        public static string? FindFile(string fileName, bool searchCurrentDirectory, IReadOnlyList<string>? searchPath)
        {
            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }

            string? resolvedPath;
            if (searchCurrentDirectory && TryResolveRelativePath(fileName, Environment.CurrentDirectory, out resolvedPath))
            {
                return resolvedPath;
            }

            if (searchPath != null && !ContainsPathSeparator(fileName))
            {
                for (int i = 0; i < searchPath.Count; i++)
                {
                    if (TryResolveRelativePath(fileName, searchPath[i], out resolvedPath))
                    {
                        return resolvedPath;
                    }
                }
            }

            return null;
        }

        private static bool ContainsPathSeparator(ReadOnlySpan<char> path)
        {
            return path.Contains(Path.DirectorySeparatorChar)
                || (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar && path.Contains(Path.AltDirectorySeparatorChar));
        }

        private static bool TryResolveRelativePath(string fileName, ReadOnlySpan<char> baseDir, [NotNullWhen(true)] out string? resolvedPath)
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

        /// <summary>
        /// <para>Split the value of the PATH environment variable to an array of strings.</para>
        /// <para>(Windows-specific) The 32-bit Windows system directory (system32) and the Windows directory are prepended.</para>
        /// </summary>
        /// <param name="envStr">The value of the PATH environment variable.</param>
        /// <returns>Array of directories in the search path.</returns>
        public static string[] ResolveSearchPath(string? envStr)
        {
            var fragments = new string[CountValues(envStr)];

            int index = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fragments[0] = Environment.GetFolderPath(Environment.SpecialFolder.System, Environment.SpecialFolderOption.DoNotVerify);
                fragments[1] = Environment.GetFolderPath(Environment.SpecialFolder.Windows, Environment.SpecialFolderOption.DoNotVerify);
                index = 2;
            }

            var e = new PathVariableFragmentEnumerator(envStr);
            while (e.MoveNext())
            {
                fragments[index++] = new string(e.Current);
            }

            Debug.Assert(index == fragments.Length);
            return fragments;

            static int CountValues(ReadOnlySpan<char> span)
            {
                int count = 0;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    count += 2;
                }

                var e = new PathVariableFragmentEnumerator(span);
                while (e.MoveNext())
                {
                    count++;
                }

                return count;
            }
        }

        internal ref struct PathVariableFragmentEnumerator
        {
            private ReadOnlySpan<char> _remaining;
            private ReadOnlySpan<char> _current;

            public PathVariableFragmentEnumerator(ReadOnlySpan<char> envStr)
            {
                _remaining = envStr;
                _current = default;
            }

            public ReadOnlySpan<char> Current => _current;

            public bool MoveNext()
            {
                while (_remaining.Length > 0)
                {
                    var separatorIndex = _remaining.IndexOf(Path.PathSeparator);
                    _current = separatorIndex < 0 ? _remaining : _remaining.Slice(0, separatorIndex);
                    _remaining = separatorIndex < 0 ? default : _remaining.Slice(separatorIndex + 1);
                    if (_current.Length > 0)
                    {
                        return true;
                    }
                }

                _current = default;
                return false;
            }

            public void Reset() => throw new NotSupportedException();
        }
    }
}
