// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Asmichi
{
    public static class ChildProcessExamples
    {
        public static async Task Main()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await ChildProcessExamplesWindows.Run();
            }
            else
            {
                await ChildProcessExamplesUnix.Run();
            }
        }
    }
}
