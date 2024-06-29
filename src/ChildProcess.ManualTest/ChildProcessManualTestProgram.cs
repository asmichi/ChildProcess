// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

namespace Asmichi
{
    public static class ChildProcessManualTestProgram
    {
        public static void Main()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ChildProcessManualTestProgramWindows.Run();
            }
        }
    }
}
