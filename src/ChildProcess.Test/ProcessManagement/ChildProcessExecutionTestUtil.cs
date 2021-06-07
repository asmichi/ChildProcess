// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Asmichi.ProcessManagement
{
    public static class ChildProcessExecutionTestUtil
    {
        public static string ExecuteForStandardOutput(ChildProcessStartInfo si, Encoding? encoding = null)
        {
            using var p = ChildProcess.Start(si);

            if (p.HasStandardInput)
            {
                p.StandardInput.Close();
            }

            if (p.HasStandardError)
            {
                p.StandardError.Close();
            }

            using var sr = new StreamReader(p.StandardOutput, encoding ?? Encoding.UTF8);
            var standardOutput = sr.ReadToEnd();
            p.WaitForExit();

            if (!p.IsSuccessful)
            {
                throw new ChildProcessFailedException($"Child process failed with exit code {p.ExitCode} (0x{p.ExitCode:X8}).");
            }

            return standardOutput;
        }
    }

    public class ChildProcessFailedException : System.Exception
    {
        public ChildProcessFailedException()
        {
        }

        public ChildProcessFailedException(string message)
            : base(message)
        {
        }

        public ChildProcessFailedException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected ChildProcessFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
