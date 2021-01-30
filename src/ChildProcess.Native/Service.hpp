// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "UniqueResource.hpp"
#include <cstdint>
#include <pthread.h>

struct ChildExitNotification
{
    uint64_t Token;
    // ProcessID
    int32_t ProcessID;
    // Exit status or signal number
    int32_t Status;
    // Code : CLD_EXITED, CLD_KILLED, CLD_DUMPED
    int32_t Code;
    int32_t Padding1;
};

[[nodiscard]] int ServiceMain(int mainChannelFd);
[[nodiscard]] bool NotifyServiceOfChildRegistration();

// Interface for the signal handler.
void NotifyServiceOfSignal(int signum);
