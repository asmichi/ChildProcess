// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <cerrno>
#include <cstdint>
#include <cstdio>
#include <cstdlib>

namespace
{
    BOOL WINAPI CtrlHandler(DWORD dwCtrlType)
    {
        switch (dwCtrlType)
        {
        case CTRL_C_EVENT:
            std::fprintf(stdout, "I");
            std::fflush(stdout);
            return TRUE;

        case CTRL_CLOSE_EVENT:
        case CTRL_BREAK_EVENT:
            std::fprintf(stdout, "T");
            std::fflush(stdout);
            return TRUE;

        default:
            return FALSE;
        }
    }
} // namespace

int TestCommandReportSignal(int argc, const char* const* argv)
{
    if (!SetConsoleCtrlHandler(CtrlHandler, TRUE))
    {
        std::abort();
    }

    // Tell the parent we are ready.
    std::fprintf(stdout, "R");
    std::fflush(stdout);

    // Wait for stdin to be closed.
    // NOTE: If stdin is not redirected, this getc call weirdly returns EOF on CTRL+C (Win10 20H2 19042.804).
    while (getc(stdin) != EOF)
    {
    }

    return 0;
}
