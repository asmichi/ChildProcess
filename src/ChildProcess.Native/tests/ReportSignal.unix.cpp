// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "TestSignalHandler.hpp"
#include <cerrno>
#include <cstdint>
#include <cstdio>
#include <signal.h>
#include <unistd.h>

namespace
{
    void SignalHandler(int signum)
    {
        ssize_t bytes;

        switch (signum)
        {
        case SIGINT:
            bytes = write(STDOUT_FILENO, "I", 1);
            break;

        case SIGTERM:
            bytes = write(STDOUT_FILENO, "T", 1);
            break;

        default:
            break;
        }
    }
} // namespace

int TestCommandReportSignal(int argc, const char* const* argv)
{
    SetSignalHandler(SIGINT, SA_RESTART, SignalHandler);
    SetSignalHandler(SIGTERM, SA_RESTART, SignalHandler);

    // Tell the parent we are ready.
    std::fprintf(stdout, "R");
    std::fflush(stdout);

    // Wait for stdin to be closed.
    while (getc(stdin) != EOF)
    {
    }

    return 0;
}
