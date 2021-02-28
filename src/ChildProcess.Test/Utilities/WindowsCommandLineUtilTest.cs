// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Asmichi.Utilities
{
    public class CommandLineUtilTest
    {
        [Fact]
        public void MakeCommandLineQuotesArguments()
        {
            // NOTE: in the following asserts, we substitute ' for " to ease escaping.

            // no need for quoting
            AssertCommandLine("cmd 1 2 3", "cmd", "1", "2", "3");
            AssertCommandLine(@"c\m\d\ \1\2\3\", @"c\m\d\", @"\1\2\3\");

            // spaces, tabs
            AssertCommandLine("'c m d' '1 2' a", "c m d", "1 2", "a");
            AssertCommandLine("'c\tm\td' '1\t2' a", "c\tm\td", "1\t2", "a");
            AssertCommandLine("'c m d' ' 1 2 ' a", "c m d", " 1 2 ", "a");
            AssertCommandLine("'c\tm\td' '\t1\t2\t' a", "c\tm\td", "\t1\t2\t", "a");

            // quotes
            AssertCommandLine(@"'\'cmd\'' '\'1\''", "'cmd'", "'1'");

            // backslashes in a quoted part (no need for escape)
            AssertCommandLine(@"'c m\d' '1 2\3'", @"c m\d", @"1 2\3");

            // trailing backslash in a quoted part.
            AssertCommandLine(@"'c md\\' '1 23\\'", @"c md\", @"1 23\");

            // backslashes followed by a double quote
            AssertCommandLine(@"'cmd\\\'' '123\\\''", @"cmd\'", @"123\'");
            AssertCommandLine(@"'cmd\\\\\'' '123\\\\\''", @"cmd\\'", @"123\\'");

            static void AssertCommandLine(string expected, string fileName, params string[] args)
            {
                expected = ReplaceBack(expected);
                fileName = ReplaceBack(fileName);
                args = ReplaceBack(args);

                var commandLine = WindowsCommandLineUtil.MakeCommandLine(fileName, args, shouldQuoteArguments: true).ToString();

                Assert.Equal(expected, commandLine);
                Assert.Equal(new[] { fileName }.Concat(args), ParseCommandLine(commandLine));
            }
        }

        [Fact]
        public void CanDisableQuoting()
        {
            // NOTE: in the following asserts, we substitute ' for " to ease escaping.

            // FileName should still be quoted. Arguments should not be quoted.
            AssertCommandLine(@"'\'cmd\'' '1 2 3'", "'cmd'", "'1", "2", "3'");
            AssertCommandLine(@"'\'cmd\'' '1   2   3'", "'cmd'", "'1 ", " 2 ", " 3'");

            static void AssertCommandLine(string expected, string fileName, params string[] args)
            {
                expected = ReplaceBack(expected);
                fileName = ReplaceBack(fileName);
                args = ReplaceBack(args);

                var commandLine = WindowsCommandLineUtil.MakeCommandLine(fileName, args, shouldQuoteArguments: false).ToString();

                Assert.Equal(expected, commandLine);
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

        private static string[] ReplaceBack(string[] ss) => ss.Select(ReplaceBack).ToArray();
        private static string ReplaceBack(string s) => s.Replace('\'', '"');
    }
}
