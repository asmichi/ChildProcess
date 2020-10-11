// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Service.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
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

    struct ChildCreationNotification
    {
        int Pid;
        // Indicates whether execve was successful.
        // We should not report the exit status of such a child because we already reported failure.
        bool ExecSuccessful;
        std::int64_t Token;
    };

    struct ChildProcessData
    {
        ChildCreationNotification CCN;
        siginfo_t SigInfo;
    };

    int g_SignalDataPipeReadEnd;
    int g_SignalDataPipeWriteEnd;
    int g_ChildCreationPipeReadEnd;
    int g_ChildCreationPipeWriteEnd;
    std::unique_ptr<AncillaryDataSocket> g_MainChannel;
    std::unordered_map<int, ChildProcessData> g_ChildProcesses;
} // namespace

void SetupService(int mainChannelFd);
[[nodiscard]] bool HandleSignalDataPipeInput();
[[nodiscard]] bool HandleSigchld();
[[nodiscard]] bool HandleChildCreationPipeInput();
[[nodiscard]] bool HandleMainChannelInput();
[[nodiscard]] bool HandleMainChannelOutput();
[[nodiscard]] bool NotifyChildExited(std::unordered_map<int, ChildProcessData>::iterator it);

void SetupService(int mainChannelFd)
{
    g_MainChannel = std::make_unique<AncillaryDataSocket>(mainChannelFd);

    {
        auto maybePipe = CreatePipe();
        if (!maybePipe)
        {
            FatalErrorAbort(errno, "pipe2");
        }

        g_ChildCreationPipeReadEnd = maybePipe->ReadEnd.Release();
        g_ChildCreationPipeWriteEnd = maybePipe->WriteEnd.Release();
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

void WriteToSignalDataPipe(const void* buf, size_t len)
{
    if (!WriteExactBytes(g_SignalDataPipeWriteEnd, buf, len)
        && errno != EPIPE)
    {
        // Just abort; almost nothing can be done in a signal handler.
        abort();
    }
}

[[nodiscard]] bool WriteToChildCreationPipe(int pid, bool execSuccessful, int64_t token)
{
    ChildCreationNotification ccn{};
    ccn.Pid = pid;
    ccn.ExecSuccessful = execSuccessful;
    ccn.Token = token;
    if (!WriteExactBytes(g_ChildCreationPipeWriteEnd, &ccn, sizeof(ccn)))
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
    fds[PollIndexChildCreation].fd = g_ChildCreationPipeReadEnd;
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
            if (!HandleChildCreationPipeInput())
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
        return HandleSigchld();

    default:
        // Ignored
        break;
    }

    return true;
}

bool HandleSigchld()
{
    // Because SIGCHLD is a standard signal, only one SIGCHLD signal can be queued.
    // If the queue already has an instance, further SIGCHLD signals will be "lost".
    // We need to reap all terminated children on every SIGCHLD signal.
    while (true)
    {
        siginfo_t siginfo{};
        assert(siginfo.si_pid == 0);
        int ret = waitid(P_ALL, 0, &siginfo, WEXITED | WNOHANG);
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
            return true;
        }

        auto it = g_ChildProcesses.find(pid);
        if (it == g_ChildProcesses.end())
        {
            // Delay termination handling until we receive the creation notification.
            ChildProcessData childProcess{};
            childProcess.SigInfo = siginfo;
            g_ChildProcesses.insert(std::pair{pid, childProcess});
        }
        else
        {
            it->second.SigInfo = siginfo;
            if (!NotifyChildExited(it))
            {
                return false;
            }
        }
    }
}

bool HandleChildCreationPipeInput()
{
    ChildCreationNotification ccn;
    if (!ReadExactBytes(g_ChildCreationPipeReadEnd, &ccn, sizeof(ccn)))
    {
        FatalErrorAbort(errno, "read");
    }

    auto it = g_ChildProcesses.find(ccn.Pid);
    if (it == g_ChildProcesses.end())
    {
        ChildProcessData data{};
        data.CCN = ccn;
        g_ChildProcesses.insert(std::pair{ccn.Pid, data});
        return true;
    }
    else
    {
        // Handle delayed termination notification.
        it->second.CCN = ccn;
        return NotifyChildExited(it);
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

bool NotifyChildExited(std::unordered_map<int, ChildProcessData>::iterator it)
{
    const auto data = it->second;

    g_ChildProcesses.erase(it);

    if (data.CCN.ExecSuccessful)
    {
        ChildExitNotification cen{};
        cen.Token = data.CCN.Token;
        cen.ProcessID = data.SigInfo.si_pid;
        cen.Code = data.SigInfo.si_code;
        cen.Status = data.SigInfo.si_status;
        if (!g_MainChannel->SendBuffered(&cen, sizeof(cen), BlockingFlag::NonBlocking))
        {
            TRACE_INFO("Main channel disconnected: send %d\n", errno);
            return false;
        }
    }

    return true;
}
