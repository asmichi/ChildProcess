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

void Service::Initialize(UniqueFd mainChannelFd)
{
    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        notificationPipeReadEnd_ = maybePipe->ReadEnd.Release();
        notificationPipeWriteEnd_ = maybePipe->WriteEnd.Release();
    }

    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        cancellationPipeReadEnd_ = maybePipe->ReadEnd.Release();
        cancellationPipeWriteEnd_ = maybePipe->WriteEnd.Release();
    }

    mainChannel_ = std::make_unique<AncillaryDataSocket>(std::move(mainChannelFd), cancellationPipeReadEnd_);

    SetupSignalHandlers();
}

void Service::NotifySignal(int signum)
{
    NotificationToService n;

    switch (signum)
    {
    case SIGQUIT:
        n = NotificationToService::Quit;
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

void Service::NotifyChildRegistration()
{
    if (!WriteNotification(NotificationToService::ReapRequest))
    {
        FatalErrorAbort("write");
    }
}

void Service::NotifySubchannelClosed(Subchannel* pSubchannel)
{
    subchannelCollection_.Delete(pSubchannel);

    // Wake up the main loop.
    if (!WriteNotification(NotificationToService::SubchannelClosed))
    {
        FatalErrorAbort("write");
    }
}

bool Service::WriteNotification(NotificationToService notification)
{
    return WriteExactBytes(notificationPipeWriteEnd_, &notification, sizeof(notification));
}

int Service::Run()
{
    // Main service loop
    pollfd fds[PollFdCount]{};
    fds[PollIndexNotification].fd = notificationPipeReadEnd_;
    fds[PollIndexMainChannel].fd = mainChannel_->GetFd();

    while (!ShouldExit())
    {
        fds[PollIndexNotification].events = POLLIN;
        fds[PollIndexMainChannel].events = POLLIN | (mainChannel_->HasPendingData() ? POLLOUT : 0);

        // Ignore the main channel while shutting down.
        static_assert(PollIndexMainChannel == PollFdCount - 1);
        int count = poll_restarting(fds, shuttingDown_ ? PollFdCount - 1 : PollFdCount, -1);
        if (count == -1)
        {
            FatalErrorAbort(errno, "poll");
        }

        if (fds[PollIndexNotification].revents & POLLIN)
        {
            HandleNotificationPipeInput();
        }

        if (!shuttingDown_)
        {
            if (fds[PollIndexMainChannel].revents & POLLIN)
            {
                HandleMainChannelInput();
            }

            if (fds[PollIndexMainChannel].revents & POLLOUT)
            {
                HandleMainChannelOutput();
            }

            if (fds[PollIndexMainChannel].revents & POLLHUP)
            {
                // Connection closed.
                InitiateShutdown();
            }
        }
    }

    g_ChildProcessStateMap.AutoTerminateAll();

    return 0;
}

void Service::InitiateShutdown()
{
    if (!shuttingDown_)
    {
        shuttingDown_ = true;
        mainChannel_->Shutdown();
        close(cancellationPipeWriteEnd_);
    }
}

bool Service::ShouldExit()
{
    if (!shuttingDown_)
    {
        return false;
    }
    else
    {
        return subchannelCollection_.Size() == 0;
    }
}

void Service::HandleNotificationPipeInput()
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
        case NotificationToService::Quit:
            TRACE_FATAL("Caught SIGQUIT.\n");
            // We do not have any critical data to persist. Just quit by reraising the signal.
            RaiseQuitOnSelf();
            break;

        case NotificationToService::ReapRequest:
            // PERF: We do not want to reap multiple times in one wake-up.
            hasReapRequest = true;
            break;

        case NotificationToService::SubchannelClosed:
            // Just for waking up the main loop.
            break;

        default:
            FatalErrorAbort("Internal error");
        }
    }

    if (hasReapRequest)
    {
        ReapAllExitedChildren();
    }
}

void Service::ReapAllExitedChildren()
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
                return;
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
            return;
        }

        auto pState = g_ChildProcessStateMap.GetByPid(pid);
        if (!pState)
        {
            // This child process was killed before we register it to the map.
            // Delay the reaping process until we register it and send a reap request.
            return;
        }

        NotifyClientOfExitedChild(pState.get(), siginfo);

        g_ChildProcessStateMap.Delete(pState.get());

        // We have updated our data and are ready for recycling of the PID. Reap the child.
        pState->Reap();
    }
}

void Service::HandleMainChannelInput()
{
    if (shuttingDown_)
    {
        return;
    }

    std::byte dummy;
    const ssize_t bytesReceived = mainChannel_->Recv(&dummy, 1, BlockingFlag::Blocking);
    if (!HandleRecvResult(BlockingFlag::Blocking, "recvmsg", bytesReceived, errno))
    {
        // Connection closed.
        TRACE_INFO("Main channel disconnected: recv (%zd, %d)\n", bytesReceived, errno);
        InitiateShutdown();
        return;
    }

    auto maybeSubchannelFd = mainChannel_->PopReceivedFd();
    if (!maybeSubchannelFd)
    {
        TRACE_FATAL("The counterpart sent a subchannel creation request but dit not send any fd.\n");
        InitiateShutdown();
        return;
    }

    auto pBorrowedSubchannel = subchannelCollection_.Add(std::make_unique<Subchannel>(std::move(*maybeSubchannelFd), cancellationPipeReadEnd_));
    if (!pBorrowedSubchannel->StartCommunicationThread())
    {
        subchannelCollection_.Delete(pBorrowedSubchannel);
    }
}

void Service::HandleMainChannelOutput()
{
    if (shuttingDown_)
    {
        return;
    }

    if (!mainChannel_->Flush(BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: fflush %d\n", errno);
        InitiateShutdown();
    }
}

void Service::NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo)
{
    if (shuttingDown_)
    {
        return;
    }

    ChildExitNotification cen{};
    cen.Token = pState->GetToken();
    cen.ProcessID = pState->GetPid();

    if (siginfo.si_code == CLD_EXITED)
    {
        cen.Status = siginfo.si_status;
    }
    else
    {
        // On macOS prior to 11.0, for killed processes, waitid (not waitpid) returns 0 in siginfo_t.si_status.
        // We need WNOWAIT to be resistant to PID recycling, hence no luck. Just return -1 on affected versions of macOS.
        cen.Status = siginfo.si_status == 0 ? -1 : -siginfo.si_status;
    }

    if (!mainChannel_->SendBuffered(&cen, sizeof(cen), BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: send %d\n", errno);
        InitiateShutdown();
    }
}
