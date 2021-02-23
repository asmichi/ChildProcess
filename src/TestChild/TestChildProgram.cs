// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Asmichi.Utilities
{
    internal static class TestChildProgram
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Write("TestChild");
                return 0;
            }

            var command = args[0];
            switch (command)
            {
                case "ExitCode":
                    return CommandExitCode(args);
                case "EchoOutAndError":
                    return CommandEchoOutAndError();
                case "EchoBack":
                    return CommandEchoBack();
                case "Sleep":
                    return CommandSleep(args);
                case "DumpEnvironmentVariables":
                    return CommandDumpEnvironmentVariables();
                case "EchoWorkingDirectory":
                    return CommandEchoWorkingDirectory();
                case "EchoCodePage":
                    return CommandEchoCodePage();
                default:
                    Console.WriteLine("Unknown command: {0}", command);
                    return 1;
            }
        }

        private static int CommandExitCode(string[] args)
        {
            return int.Parse(args[1], CultureInfo.InvariantCulture);
        }

        private static int CommandEchoOutAndError()
        {
            Console.Write("TestChild.Out");
            Console.Error.Write("TestChild.Error");
            return 0;
        }

        private static int CommandEchoBack()
        {
            var text = Console.In.ReadToEnd();
            Console.Write(text);

            return 0;
        }

        private static int CommandSleep(string[] args)
        {
            int duration = int.Parse(args[1], CultureInfo.InvariantCulture);
            Thread.Sleep(duration);
            return 0;
        }

        private static int CommandDumpEnvironmentVariables()
        {
            // Output in UTF-8 so that the output will not be affected by the current code page.
            using var sw = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8);

            var evars = Environment.GetEnvironmentVariables();
            foreach (var key in evars.Keys.Cast<string>().OrderBy(x => x))
            {
                sw.Write("{0}={1}\0", key, (string?)evars[key]);
            }

            return 0;
        }

        private static int CommandEchoWorkingDirectory()
        {
            Console.Write(Environment.CurrentDirectory);
            return 0;
        }

        private static int CommandEchoCodePage()
        {
            int codePage = Kernel32.GetConsoleOutputCP();
            if (codePage == 0)
            {
                return Marshal.GetLastWin32Error();
            }

            Console.Write("{0}", codePage);
            return 0;
        }
    }
}
