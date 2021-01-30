// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "SignalHandler.hpp"
#include "Base.hpp"
#include "MiscHelpers.hpp"
#include "Service.hpp"
#include "UniqueResource.hpp"
#include <cassert>
#include <cstdint>
#include <cstring>
#include <signal.h>
#include <unistd.h>

bool IsSignalIgnored(int signum);
void SetSignalAction(int signum, int extraFlags);
void SignalHandler(int signum, siginfo_t* siginfo, void* context);

void SetupSignalHandlers()
{
    // Preserve the ignored state as far as possible so that our children will inherit the state.
    if (!IsSignalIgnored(SIGINT))
    {
        SetSignalAction(SIGINT, 0);
    }
    if (!IsSignalIgnored(SIGQUIT))
    {
        SetSignalAction(SIGQUIT, 0);
    }
    if (!IsSignalIgnored(SIGPIPE))
    {
        SetSignalAction(SIGPIPE, 0);
    }

    SetSignalAction(SIGCHLD, SA_NOCLDSTOP);
}

bool IsSignalIgnored(int signum)
{
    struct sigaction oldact;
    [[maybe_unused]] int isError = sigaction(signum, nullptr, &oldact);
    assert(isError == 0);
    return oldact.sa_handler == SIG_IGN;
}

void SetSignalAction(int signum, int extraFlags)
{
    struct sigaction act = {};
    act.sa_flags = SA_RESTART | SA_SIGINFO | extraFlags;
    sigemptyset(&act.sa_mask);
    act.sa_sigaction = SignalHandler;

    [[maybe_unused]] int isError = sigaction(signum, &act, nullptr);
    assert(isError == 0);
}

void SignalHandler(int signum, siginfo_t* siginfo, void* context)
{
    // Avoid doing the real work in the signal handler.
    // Dispatch the real work to the service thread.
    switch (signum)
    {
    case SIGINT:
    case SIGQUIT:
    case SIGCHLD:
    {
        const int err = errno;
        NotifyServiceOfSignal(signum);
        errno = err;
        break;
    }

    case SIGPIPE:
    default:
        // Ignored
        break;
    }
}
