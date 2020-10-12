// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    public class CommandLineUtilTest
    {
        [Fact]
        public void MakeCommandLineQuotesArguments()
        {
            // NOTE: in the following asserts, we substitute ' for " to ease escaping.

            // no need for quoting
            Assert("cmd 1 2 3", "cmd", "1", "2", "3");
            Assert(@"c\m\d\ \1\2\3\", @"c\m\d\", @"\1\2\3\");

            // spaces, tabs
            Assert("'c m d' '1 2' a", "c m d", "1 2", "a");
            Assert("'c\tm\td' '1\t2' a", "c\tm\td", "1\t2", "a");
            Assert("'c m d' ' 1 2 ' a", "c m d", " 1 2 ", "a");
            Assert("'c\tm\td' '\t1\t2\t' a", "c\tm\td", "\t1\t2\t", "a");

            // quotes
            Assert(@"'\'cmd\'' '\'1\''", "'cmd'", "'1'");

            // backslashes in a quoted part (no need for escape)
            Assert(@"'c m\d' '1 2\3'", @"c m\d", @"1 2\3");

            // trailing backslash in a quoted part.
            Assert(@"'c md\\' '1 23\\'", @"c md\", @"1 23\");

            // backslashes followed by a double quote
            Assert(@"'cmd\\\'' '123\\\''", @"cmd\'", @"123\'");
            Assert(@"'cmd\\\\\'' '123\\\\\''", @"cmd\\'", @"123\\'");

            static void Assert(string expected, string fileName, params string[] args)
            {
                static string Replace(string s) => s.Replace('\'', '"');

                var commandLine = WindowsCommandLineUtil.MakeCommandLine(Replace(fileName), args.Select(Replace).ToArray()).ToString();

                Xunit.Assert.Equal(Replace(expected), commandLine);
                Xunit.Assert.Equal(new[] { fileName }.Concat(args).Select(Replace).ToArray(), ParseCommandLine(commandLine).ToArray());
            }
        }

        // NOTE: This implementation does not handle escape chars outside a quoted part. ex) a\"
        private static List<string> ParseCommandLine(string commandLine)
        {
            bool IsDelimiter(char c) => c == ' ' || c == '\t';

            var sb = new StringBuilder();
            var result = new List<string>();

            // Trim away preceeding and trailing delimiters.
            int i = commandLine.TakeWhile(IsDelimiter).Count();
            if (i == commandLine.Length)
            {
                return result;
            }

            int endIndex = commandLine.Length - commandLine.Reverse().TakeWhile(IsDelimiter).Count();

            // split and unquote
            while (true)
            {
                if (i == endIndex)
                {
                    result.Add(sb.ToString());
                    return result;
                }

                var c = commandLine[i++];

                if (IsDelimiter(c))
                {
                    // end of an argument
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    // beginning of a quoted part
                    int backslashes = 0;
                    while (true)
                    {
                        if (i == endIndex)
                        {
                            if (backslashes > 0)
                            {
                                sb.Append('\\', backslashes);
                            }
                            break;
                        }

                        var c2 = commandLine[i++];
                        if (c2 == '"')
                        {
                            if (backslashes % 2 == 0)
                            {
                                // end of a quoted part
                                sb.Append('\\', backslashes / 2);
                                break;
                            }
                            else
                            {
                                // an escaped double quote
                                if (backslashes > 1)
                                {
                                    sb.Append('\\', backslashes / 2);
                                }
                                backslashes = 0;
                                sb.Append('"');
                            }
                        }
                        else if (c2 == '\\')
                        {
                            // buffer backslashes because they may escape a double quote or backslashes
                            backslashes++;
                        }
                        else
                        {
                            // normal char
                            if (backslashes > 0)
                            {
                                sb.Append('\\', backslashes);
                                backslashes = 0;
                            }
                            sb.Append(c2);
                        }
                    }
                }
                else
                {
                    // normal char
                    sb.Append(c);
                }
            }
        }
    }
}
