// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "ChildProcessState.hpp"
#include "Base.hpp"
#include <cassert>
#include <memory>
#include <mutex>
#include <signal.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>
#include <unordered_map>

void ChildProcessState::Reap()
{
    const std::lock_guard<std::mutex> guard(mutex_);
    assert(!isReaped_);
    if (isReaped_)
    {
        return;
    }

    siginfo_t siginfo;
    int ret = waitid(P_PID, pid_, &siginfo, WEXITED | WNOHANG);
    if (ret < 0)
    {
        FatalErrorAbort(errno, "waitpid");
    }

    isReaped_ = true;
}

bool ChildProcessState::SendSignal(int sig, bool alsoSendSigCont) const
{
    const std::lock_guard<std::mutex> guard(mutex_);
    if (isReaped_)
    {
        return true;
    }

    const int target = isNewProcessGroup_ ? -pid_ : pid_;
    const int ret = kill(target, sig);
    if (ret == 0 && alsoSendSigCont)
    {
        int err = errno;
        kill(target, SIGCONT);
        errno = err;
    }
    return ret == 0;
}

void ChildProcessStateMap::Allocate(int pid, std::uint64_t token, bool isNewProcessGroup, bool shouldAutoTerminate)
{
    const auto pState = std::make_shared<ChildProcessState>(pid, token, isNewProcessGroup, shouldAutoTerminate);

    const std::lock_guard<std::mutex> guard(mapMutex_);

    const auto [pidIt, pidInserted] = byPid_.insert(std::pair{pid, pState});
    const auto [tokenIt, tokenInserted] = byToken_.insert(std::pair{token, pState});

    assert(pidInserted && tokenInserted);
    if (!pidInserted)
    {
        FatalErrorAbort("We must not reap a child before we remove its PID from the map.");
    }
}

std::shared_ptr<ChildProcessState> ChildProcessStateMap::GetByPid(int pid) const
{
    const std::lock_guard<std::mutex> guard(mapMutex_);
    const auto it = byPid_.find(pid);
    if (it == byPid_.end())
    {
        return {};
    }
    else
    {
        return it->second;
    }
}

std::shared_ptr<ChildProcessState> ChildProcessStateMap::GetByToken(std::uint64_t token) const
{
    const std::lock_guard<std::mutex> guard(mapMutex_);
    const auto it = byToken_.find(token);
    if (it == byToken_.end())
    {
        return {};
    }
    else
    {
        return it->second;
    }
}

void ChildProcessStateMap::Delete(ChildProcessState* pState)
{
    const auto pid = pState->GetPid();
    const auto token = pState->GetToken();

    const std::lock_guard<std::mutex> guard(mapMutex_);

    const auto pidIt = byPid_.find(pid);
    assert(pidIt != byPid_.end());

    const auto tokenIt = byToken_.find(token);
    assert(tokenIt != byToken_.end());

    byPid_.erase(pidIt);
    byToken_.erase(tokenIt);
}

void ChildProcessStateMap::AutoTerminateAll()
{
    const std::lock_guard<std::mutex> guard(mapMutex_);

    for (const auto& it : byToken_)
    {
        const auto& childProcess = it.second;
        if (childProcess->ShouldAutoTerminate())
        {
            TRACE_INFO("Auto-terminating PID %d.\n", childProcess->GetPid());
            if (!childProcess->SendSignal(SIGTERM, true) && errno != ESRCH)
            {
                TRACE_ERROR("Failed to auto-terminate %d (%d).", childProcess->GetPid(), errno);
            }
        }
    }
}
