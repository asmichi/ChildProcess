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
    enum
    {
        PollIndexSignalData = 0,
        PollIndexChildCreation = 1,
        PollIndexMainChannel = 2,
    };
    const int PollFdCount = 3;

    static_assert(sizeof(pid_t) == sizeof(int));

    // The signal handler writes signal numbers here (except SIGCHLD, which will be written to g_ReapRequestPipeWriteEnd).
    int g_SignalDataPipeReadEnd;
    int g_SignalDataPipeWriteEnd;

    // Subchannels will write a dummy byte here to request the service to reap children. SIGCHLD will also be written here.
    int g_ReapRequestPipeReadEnd;
    int g_ReapRequestPipeWriteEnd;

    std::unique_ptr<AncillaryDataSocket> g_MainChannel;
} // namespace

void SetupService(int mainChannelFd);
[[nodiscard]] bool HandleSignalDataPipeInput();
[[nodiscard]] bool HandleReapRequestPipeInput();
[[nodiscard]] bool HandleReapRequest();
[[nodiscard]] bool HandleMainChannelInput();
[[nodiscard]] bool HandleMainChannelOutput();
[[nodiscard]] bool NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo);

void SetupService(int mainChannelFd)
{
    g_MainChannel = std::make_unique<AncillaryDataSocket>(mainChannelFd);

    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        g_ReapRequestPipeReadEnd = maybePipe->ReadEnd.Release();
        g_ReapRequestPipeWriteEnd = maybePipe->WriteEnd.Release();
    }

    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        g_SignalDataPipeReadEnd = maybePipe->ReadEnd.Release();
        g_SignalDataPipeWriteEnd = maybePipe->WriteEnd.Release();
    }

    SetupSignalHandlers();
}

void NotifyServiceOfSignal(int signum)
{
    if (signum == SIGCHLD)
    {
        std::byte dummy{};
        if (!WriteExactBytes(g_ReapRequestPipeWriteEnd, &dummy, 1)
            && errno != EPIPE)
        {
            // Just abort; almost nothing can be done in a signal handler.
            abort();
        }
    }
    else
    {
        if (!WriteExactBytes(g_SignalDataPipeWriteEnd, &signum, sizeof(signum))
            && errno != EPIPE)
        {
            // Just abort; almost nothing can be done in a signal handler.
            abort();
        }
    }
}

[[nodiscard]] bool NotifyServiceOfChildRegistration()
{
    std::uint8_t dummy = 0;
    if (!WriteExactBytes(g_ReapRequestPipeWriteEnd, &dummy, 1))
    {
        // Pretend successful on EPIPE. The data will not be read anyway because the service is shutting down.
        return errno == EPIPE;
    }

    return true;
}

int ServiceMain(int mainChannelFd)
{
    SetupService(mainChannelFd);

    // Main service loop
    pollfd fds[PollFdCount]{};
    fds[PollIndexSignalData].fd = g_SignalDataPipeReadEnd;
    fds[PollIndexChildCreation].fd = g_ReapRequestPipeReadEnd;
    fds[PollIndexMainChannel].fd = g_MainChannel->GetFd();

    while (true)
    {
        fds[PollIndexSignalData].events = POLLIN;
        fds[PollIndexChildCreation].events = POLLIN;
        fds[PollIndexMainChannel].events = POLLIN | (g_MainChannel->HasPendingData() ? POLLOUT : 0);

        int count = poll_restarting(fds, PollFdCount, -1);
        if (count == -1)
        {
            FatalErrorAbort(errno, "poll");
        }

        if (fds[PollIndexSignalData].revents & POLLIN)
        {
            if (!HandleSignalDataPipeInput())
            {
                return 1;
            }
        }

        if (fds[PollIndexChildCreation].revents & POLLIN)
        {
            if (!HandleReapRequestPipeInput())
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

bool HandleSignalDataPipeInput()
{
    int signum;
    ssize_t readBytes;

    if (!ReadExactBytes(g_SignalDataPipeReadEnd, &signum, sizeof(int)))
    {
        FatalErrorAbort(errno, "read");
    }

    switch (signum)
    {
    case SIGINT:
    case SIGQUIT:
        // Do some cleanup (if any) and exit.
        TRACE_INFO("Caught signal %d\n", signum);
        return false;

    case SIGCHLD:
        // SIGCHLD must be sent as a reap request.
        assert(false);
        break;

    default:
        // Ignored
        break;
    }

    return true;
}

bool HandleReapRequestPipeInput()
{
    // Drain data from the pipe. If we have more than 256 bytes of pending data, we just re-poll and reexecute this.
    std::byte buf[256];
    if (read_restarting(g_ReapRequestPipeReadEnd, buf, sizeof(buf)) == -1)
    {
        FatalErrorAbort(errno, "read");
    }

    return HandleReapRequest();
}

bool HandleReapRequest()
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

bool HandleMainChannelInput()
{
    std::byte dummy;
    const ssize_t bytesReceived = g_MainChannel->Recv(&dummy, 1, BlockingFlag::Blocking);
    if (!HandleRecvResult(BlockingFlag::Blocking, "recvmsg", bytesReceived, errno))
    {
        // Connection closed.
        TRACE_INFO("Main channel disconnected: recv %d\n", errno);
        return false;
    }

    auto maybeSubchannelFd = g_MainChannel->PopReceivedFd();
    if (!maybeSubchannelFd)
    {
        TRACE_FATAL("The counterpart sent a subchannel creation request but dit not send any fd.\n");
        return false;
    }

    StartSubchannelHandler(std::move(*maybeSubchannelFd));
    return true;
}

bool HandleMainChannelOutput()
{
    if (!g_MainChannel->Flush(BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: fflush %d\n", errno);
        return false;
    }

    return true;
}

bool NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo)
{
    ChildExitNotification cen{};
    cen.Token = pState->GetToken();
    cen.ProcessID = pState->GetPid();
    cen.Code = siginfo.si_code;
    cen.Status = siginfo.si_status;
    if (!g_MainChannel->SendBuffered(&cen, sizeof(cen), BlockingFlag::NonBlocking))
    {
        TRACE_INFO("Main channel disconnected: send %d\n", errno);
        return false;
    }

    return true;
}
