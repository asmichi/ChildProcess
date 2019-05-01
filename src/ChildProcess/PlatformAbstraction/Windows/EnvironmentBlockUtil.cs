// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Diagnostics;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal static class EnvironmentBlockUtil
    {
        /// <summary>
        /// Constructs an environment block for CreateProcess.
        /// </summary>
        /// <param name="evars">Collection of environment variables.</param>
        /// <returns>A string that contains the environment block.</returns>
        public static char[] MakeEnvironmentBlockWin32(IReadOnlyCollection<(string name, string value)> evars)
        {
            var buf = new char[CalculateLength(evars)];

            int cur = 0;
            foreach (var (name, value) in evars)
            {
                // name=value\0
                name.CopyTo(0, buf, cur, name.Length);
                cur += name.Length;

                buf[cur++] = '=';

                value.CopyTo(0, buf, cur, value.Length);
                cur += value.Length;

                buf[cur++] = '\0';
            }

            // Terminating \0
            buf[cur++] = '\0';

            Debug.Assert(cur == buf.Length);

            return buf;
        }

        private static int CalculateLength(IReadOnlyCollection<(string name, string value)> evars)
        {
            int length = 0;
            foreach (var (name, value) in evars)
            {
                // name=value\0
                length += name.Length + value.Length + 2;
            }

            // Terminating \0
            length++;

            return length;
        }
    }
}
