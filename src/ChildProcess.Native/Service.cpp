// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Service.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "ChildProcessState.hpp"
#include "Globals.hpp"
#include "MiscHelpers.hpp"
#include "SignalHandler.hpp"
#include "SocketHelpers.hpp"
#include "Subchannel.hpp"
#include "UniqueResource.hpp"
#include "WriteBuffer.hpp"
#include <cassert>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <memory>
#include <poll.h>
#include <signal.h>
#include <sys/wait.h>
#include <unistd.h>
#include <unordered_map>

static_assert(sizeof(pid_t) == sizeof(int32_t));

namespace
{
    // NOTE: Make sure to sync with the client.
    struct ChildExitNotification
    {
        uint64_t Token;
        // ProcessID
        int32_t ProcessID;
        // Exit status on CLD_EXITED; -N on CLD_KILLED and CLD_DUMPED where N is the signal number.
        int32_t Status;
    };
    static_assert(sizeof(ChildExitNotification) == 16);

    enum
    {
        PollIndexNotification = 0,
        PollIndexMainChannel = 1,
    };
    const int PollFdCount = 2;

} // namespace

void Service::Initialize(int mainChannelFd)
{
    mainChannel_ = std::make_unique<AncillaryDataSocket>(mainChannelFd);

    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        notificationPipeReadEnd_ = maybePipe->ReadEnd.Release();
        notificationPipeWriteEnd_ = maybePipe->WriteEnd.Release();
    }

    SetupSignalHandlers();
}

void Service::NotifySignal(int signum)
{
    NotificationToService n;

    switch (signum)
    {
    case SIGINT:
        n = NotificationToService::Interrupt;
        break;

    case SIGQUIT:
        n = NotificationToService::Quit;
        break;

    case SIGTERM:
        n = NotificationToService::Termination;
        break;

    case SIGCHLD:
        n = NotificationToService::ReapRequest;
        break;

    default:
        return;
    }

    if (!WriteNotification(n) && errno != EPIPE)
    {
        // Just abort; almost nothing can be done in a signal handler.
        abort();
    }
}

bool Service::NotifyChildRegistration()
{
    if (!WriteNotification(NotificationToService::ReapRequest))
    {
        // Pretend successful on EPIPE. The data will not be read anyway because the service is shutting down.
        return errno == EPIPE;
    }

    return true;
}

bool Service::WriteNotification(NotificationToService notification)
{
    return WriteExactBytes(notificationPipeWriteEnd_, &notification, sizeof(notification));
}

int Service::MainLoop(int mainChannelFd)
{
    Initialize(mainChannelFd);

    // Main service loop
    pollfd fds[PollFdCount]{};
    fds[PollIndexNotification].fd = notificationPipeReadEnd_;
    fds[PollIndexMainChannel].fd = mainChannel_->GetFd();

    while (true)
    {
        fds[PollIndexNotification].events = POLLIN;
        fds[PollIndexMainChannel].events = POLLIN | (mainChannel_->HasPendingData() ? POLLOUT : 0);

        int count = poll_restarting(fds, PollFdCount, -1);
        if (count == -1)
        {
            FatalErrorAbort(errno, "poll");
        }

        if (fds[PollIndexNotification].revents & POLLIN)
        {
            if (!HandleNotificationPipeInput())
            {
                return 1;
            }
        }

        if (fds[PollIndexMainChannel].revents & POLLIN)
        {
            if (!HandleMainChannelInput())
            {
                return 1;
            }
        }

        if (fds[PollIndexMainChannel].revents & POLLOUT)
        {
            if (!HandleMainChannelOutput())
            {
                return 1;
            }
        }

        if ((fds[PollIndexMainChannel].revents & (POLLHUP | POLLIN)) == POLLHUP)
        {
            // Connection closed.
            return 1;
        }
    }
}

bool Service::HandleNotificationPipeInput()
{
    // Drain data from the pipe. If we have more than 256 bytes of pending data, we just re-poll and reexecute this.
    NotificationToService notifications[256];
    ssize_t readSize;

    if ((readSize = read_restarting(notificationPipeReadEnd_, notifications, sizeof(notifications))) == -1)
    {
        FatalErrorAbort(errno, "read");
    }

    bool hasReapRequest = false;
    for (ssize_t i = 0; i < readSize; i++)
    {
        auto notification = notifications[i];

        switch (notification)
        {
        case NotificationToService::Interrupt:
            // TODO: Propagate SIGINT to children.
            // NOTE: It's up to the client whether the service should exit on SIGINT. (The service will exit when the connection is closed.)
            TRACE_INFO("Caught SIGINT.\n");
            return false;

        case NotificationToService::Termination:
            // TODO: Propagate SIGTERM to children.
            // NOTE: It's up to the client whether the service should exit on SIGTERM. (The service will exit when the connection is closed.)
            TRACE_INFO("Caught SIGTERM).\n");
            return false;

        case NotificationToService::Quit:
            // TODO: DO a minimal cleanup and reraise the signal to exit.
            TRACE_INFO("Caught SIGQUIT.\n");
            return false;

        case NotificationToService::ReapRequest:
            // PERF: We do not want to reap multiple times in one wake-up.
            hasReapRequest = true;
            break;

        default:
            FatalErrorAbort("Internal error");
        }
    }

    if (hasReapRequest)
    {
        if (!HandleReapRequest())
        {
            return false;
        }
    }

    return true;
}

bool Service::HandleReapRequest()
{
    // Because SIGCHLD is a standard signal, only one SIGCHLD signal can be queued.
    // If the queue already has an instance, further SIGCHLD signals will be "lost".
    // We need to reap all terminated children on every SIGCHLD signal.
    while (true)
    {
        siginfo_t siginfo{};
        assert(siginfo.si_pid == 0);

        // Peek a waitable child.
        int ret = waitid(P_ALL, 0, &siginfo, WEXITED | WNOHANG | WNOWAIT);
        if (ret < 0)
        {
            if (errno == ECHILD)
            {
                return true;
            }
            else
            {
                FatalErrorAbort(errno, "waitpid");
            }
        }

        const auto pid = siginfo.si_pid;
        if (pid == 0)
        {
            // No waitable child.
            return true;
        }

        auto pState = g_ChildProcessStateMap.GetByPid(pid);
        if (!pState)
        {
            // This child process was killed before we register it to the map.
            // Delay the reaping process until we register it and send a reap request.
            return true;
        }

        if (!NotifyClientOfExitedChild(pState.get(), siginfo))
        {
            return false;
        }

        g_ChildProcessStateMap.Delete(pState.get());

        // We have updated our data and are ready for recycling of the PID. Reap the child.
        pState->Reap();
    }
}

bool Service::HandleMainChannelInput()
{
    std::byte dummy;
    const ssize_t bytesReceived = mainChannel_->Recv(&dummy, 1, BlockingFlag::Blocking);
    if (!HandleRecvResult(BlockingFlag::Blocking, "recvmsg", bytesReceived, errno))
    {
        // Connection closed.
        TRACE_INFO("Main channel disconnected: recv %d\n", errno);
        return false;
    }

    auto maybeSubchannelFd = mainChannel_->PopReceivedFd();
    if (!maybeSubchannelFd)
    {
        TRACE_FATAL("The counterpart sent a subchannel creation request but dit not send any fd.\n");
        return false;
    }

    StartSubchannelHandler(std::move(*maybeSubchannelFd));
    return true;
}

bool Service::HandleMainChannelOutput()
{
    if (!mainChannel_->Flush(BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: fflush %d\n", errno);
        return false;
    }

    return true;
}

bool Service::NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo)
{
    ChildExitNotification cen{};
    cen.Token = pState->GetToken();
    cen.ProcessID = pState->GetPid();
    cen.Status = siginfo.si_code == CLD_EXITED ? siginfo.si_status : -siginfo.si_status;

    if (!mainChannel_->SendBuffered(&cen, sizeof(cen), BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: send %d\n", errno);
        return false;
    }

    return true;
}
