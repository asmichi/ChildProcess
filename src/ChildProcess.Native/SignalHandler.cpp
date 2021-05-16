// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "SignalHandler.hpp"
#include "Base.hpp"
#include "Globals.hpp"
#include "MiscHelpers.hpp"
#include "Service.hpp"
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
    if (!IsSignalIgnored(SIGTERM))
    {
        SetSignalAction(SIGTERM, 0);
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

void RaiseQuitOnSelf()
{
    struct sigaction act = {};
    act.sa_flags = 0;
    sigemptyset(&act.sa_mask);
    act.sa_handler = SIG_DFL;

    if (sigaction(SIGQUIT, &act, nullptr) != 0)
    {
        FatalErrorAbort("sigaction");
    }

    if (kill(getpid(), SIGQUIT) == -1)
    {
        FatalErrorAbort("kill");
    }

    std::abort();
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

void SignalHandler(int signum, siginfo_t*, void*)
{
    // Avoid doing the real work in the signal handler.
    // Dispatch the real work to the service thread.
    switch (signum)
    {
    case SIGQUIT:
    case SIGCHLD:
    {
        const int err = errno;
        g_Service.NotifySignal(signum);
        errno = err;
        break;
    }

    // NOTE: It's up to the client whether the service should exit on SIGINT/SIGTERM. (The service will exit when the connection is closed.)
    // NOTE: It's up to the client whether child processs in other process groups should be sent SIGINT/SIGTERM.
    case SIGINT:
    case SIGTERM:
    case SIGPIPE:
    default:
        // Ignored
        break;
    }
}
