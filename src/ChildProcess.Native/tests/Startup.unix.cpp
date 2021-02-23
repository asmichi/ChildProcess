// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Base.hpp"
#include "TestSignalHandler.hpp"
#include <signal.h>
#include <unistd.h>

void SetupDefaultTestSignalHandlers()
{
    SetSignalHandler(SIGINT, 0, SIG_DFL);
    SetSignalHandler(SIGTERM, 0, SIG_DFL);
    SetSignalHandler(SIGQUIT, 0, SIG_DFL);
    SetSignalHandler(SIGPIPE, 0, SIG_IGN);
    SetSignalHandler(SIGCHLD, 0, SIG_IGN);
}
