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
    // Exit status on CLD_EXITED; -N on CLD_KILLED and CLD_DUMPED where N is the signal number.
    int32_t Status;
};
static_assert(sizeof(ChildExitNotification) == 16);

[[nodiscard]] int ServiceMain(int mainChannelFd);
[[nodiscard]] bool NotifyServiceOfChildRegistration();

// Interface for the signal handler.
void NotifyServiceOfSignal(int signum);
