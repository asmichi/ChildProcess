// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "TestSignalHandler.hpp"
#include "Base.hpp"
#include <signal.h>
#include <unistd.h>

void SetSignalHandler(int signum, int flags, void (*handler)(int))
{
    struct sigaction act = {};
    act.sa_flags = flags;
    sigemptyset(&act.sa_mask);
    act.sa_handler = handler;

    if (sigaction(signum, &act, nullptr) != 0)
    {
        FatalErrorAbort("sigaction");
    }
}
