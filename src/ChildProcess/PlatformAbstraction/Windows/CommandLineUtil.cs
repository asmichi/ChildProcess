// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Text;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal static class CommandLineUtil
    {
        /// <summary>
        /// Constructs a properly quoted command line.
        /// </summary>
        /// <param name="fileName">Path to an executable.</param>
        /// <param name="args">Arguments of a command.</param>
        /// <returns>A <see cref="StringBuilder"/> that contains the command line.</returns>
        // TODO: custom quoting
        public static StringBuilder MakeCommandLine(string fileName, IReadOnlyCollection<string> args)
        {
            var sb = new StringBuilder(EstimateRequiredCapacity(fileName, args));
            AppendArgumentQuoted(sb, fileName);

            foreach (var arg in args)
            {
                sb.Append(' ');
                AppendArgumentQuoted(sb, arg);
            }

            return sb;
        }

        private static int EstimateRequiredCapacity(string fileName, IReadOnlyCollection<string> args)
        {
            // Return the length of the command line where all arguments are quoted (+2 chars).
            int capacity = 0;
            capacity += fileName.Length + 2;
            foreach (var arg in args)
            {
                capacity += 1 + arg.Length + 2;
            }
            return capacity;
        }

        // Quotes an argument according to the UCRT command line parser.
        private static void AppendArgumentQuoted(StringBuilder sb, string s)
        {
            // Technically a quoted part can start in the middle of an argument,
            // so we could quote part of the string as we iterate over the string.
            //
            // For a cosmetic reason, prefer to quote the argument as a whole.
            if (!IsQuotingRequired(s))
            {
                sb.Append(s);
                return;
            }

            sb.Append('"');

            int backslashCount = 0;
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\':
                        {
                            sb.Append(c);
                            backslashCount++;
                            break;
                        }
                    case '"':
                        {
                            // Iff backslashes are followed by a double-quote, those backslashes will be parsed as escape characters of the double-quote.
                            // Escape the backslashes.
                            sb.Append('\\', backslashCount);
                            backslashCount = 0;

                            // Escape the double-quote.
                            sb.Append("\\\""); // "\"\"" will be also valid here.
                            break;
                        }
                    default:
                        {
                            sb.Append(c);
                            backslashCount = 0;
                            break;
                        }
                }
            }

            // Escape trailing backslashes.
            sb.Append('\\', backslashCount);

            sb.Append('"');
        }

        private static bool IsQuotingRequired(string s)
        {
            // An empty string must be quoted.
            if (s.Length == 0)
            {
                return true;
            }

            foreach (char c in s)
            {
                // Characters that must reside in a quoted part:
                // - A space or a tab: Delimits arguments.
                // - A double-quote: Starts a quoted part. (NOTE: Optimally can be escaped by a backslash.)
                if (c == ' ' || c == '\t' || c == '"')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
