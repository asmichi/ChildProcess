// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Asmichi
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
                case "DumpEnvironmentVariables":
                    return CommandDumpEnvironmentVariables();
                case "EchoAndSleepAndEcho":
                    return CommandEchoAndSleepAndEcho(args);
                case "EchoBack":
                    return CommandEchoBack();
                case "EchoCodePage":
                    return CommandEchoCodePage();
                case "EchoOutAndError":
                    return CommandEchoOutAndError();
                case "EchoWorkingDirectory":
                    return CommandEchoWorkingDirectory();
                case "ExitCode":
                    return CommandExitCode(args);
                case "Sleep":
                    return CommandSleep(args);
                case "SpawnAndWait":
                    return CommandSpawnAndWait(args);
                case "ToResolvedCurrentDirectory":
                    return CommandToResolvedCurrentDirectory(args);
                default:
                    Console.WriteLine("Unknown command: {0}", command);
                    return 1;
            }
        }

        private static int CommandDumpEnvironmentVariables()
        {
            // Output in UTF-8 so that the output will not be affected by the current code page.
            using var sw = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8);

            var envVars = Environment.GetEnvironmentVariables();
            foreach (var key in envVars.Keys.Cast<string>().OrderBy(x => x, StringComparer.Ordinal))
            {
                sw.Write("{0}={1}\0", key, (string?)envVars[key]);
            }

            return 0;
        }

        private static int CommandEchoAndSleepAndEcho(string[] args)
        {
            Console.Out.Write(args[1]);
            Console.Out.Flush();

            int duration = int.Parse(args[2], CultureInfo.InvariantCulture);
            Thread.Sleep(duration);

            Console.Out.Write(args[3]);
            Console.Out.Flush();

            return 0;
        }

        private static int CommandEchoBack()
        {
            var text = Console.In.ReadToEnd();
            Console.Write(text);

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

        private static int CommandEchoOutAndError()
        {
            Console.Write("TestChild.Out");
            Console.Error.Write("TestChild.Error");
            return 0;
        }

        private static int CommandEchoWorkingDirectory()
        {
            Console.Write(Environment.CurrentDirectory);
            return 0;
        }

        private static int CommandExitCode(string[] args)
        {
            return int.Parse(args[1], CultureInfo.InvariantCulture);
        }

        private static int CommandSleep(string[] args)
        {
            int duration = int.Parse(args[1], CultureInfo.InvariantCulture);
            Thread.Sleep(duration);
            return 0;
        }

        private static int CommandSpawnAndWait(string[] args)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = args[1],
                CreateNoWindow = false,
            };

            foreach (var arg in args.Skip(2))
            {
                psi.ArgumentList.Add(arg);
            }

            using var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }

        /// <summary>
        /// Resolve the specified path as if it were obtained with getcwd.
        /// The path must exist and be a directory.
        /// </summary>
        private static int CommandToResolvedCurrentDirectory(string[] args)
        {
            Environment.CurrentDirectory = args[1];
            Console.Write(Environment.CurrentDirectory);
            return 0;
        }
    }
}
