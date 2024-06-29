// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Asmichi.ProcessManagement;

#pragma warning disable CA1849 // Call async methods when in an async method

namespace Asmichi
{
    public static class ChildProcessManualTestProgramWindows
    {
        public static void Run()
        {
            var si = new ChildProcessStartInfo("waitfor", "/T", "3", nameof(ChildProcessManualTestProgramWindows));

            // Demonstrate https://github.com/asmichi/ChildProcess/issues/2: intermittent "Application Error 0xc0000142" dialog
            // when the parent is killed (the pseudo console is closed) before a child finishes initialization.
            //
            // This will almost certanly cause that.
            for (int i = 0; i < 5; i++)
            {
                ChildProcess.Start(si).Dispose();
            }
        }
    }
}
