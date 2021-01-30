// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include <cassert>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <memory>
#include <mutex>
#include <poll.h>
#include <signal.h>
#include <sys/wait.h>
#include <unistd.h>
#include <unordered_map>

class ChildProcessState final
{
public:
    ChildProcessState(int pid, std::uint64_t token)
        : token_(token), pid_(pid), isReaped_(false) {}

    std::uint64_t GetToken() const { return token_; }
    int GetPid() const { return pid_; }
    void Reap();
    [[nodiscard]] bool SendSignal(int sig);

private:
    // Serializes all accesses to the process (signal, reap, etc.).
    // NOTE: We must not access a process after we reap it. Otherwise we are vulnerable to PID recycling.
    std::mutex mutex_;
    const std::uint64_t token_;
    const int pid_;
    bool isReaped_;
};

// Maintains ChildProcessState elements for all our children.
// An element must be allocated and deleted before we reap the corresponding child.
// NOTE: Once we fork, an element must *always* be allocated for the child. No exception.
//       The element must be deleted just before we reap the child.
//       Otherwise we are vulnerable to PID recycling.
class ChildProcessStateMap final
{
public:
    void Allocate(int pid, std::uint64_t token);
    [[nodiscard]] std::shared_ptr<ChildProcessState> GetByPid(int pid) const; // Used by the reaping process only.
    [[nodiscard]] std::shared_ptr<ChildProcessState> GetByToken(std::uint64_t token) const;
    void Delete(ChildProcessState* pState);

private:
    // Serializes lookup, insertion and removal.
    mutable std::mutex mapMutex_;
    std::unordered_map<int, std::shared_ptr<ChildProcessState>> byPid_;
    std::unordered_map<std::uint64_t, std::shared_ptr<ChildProcessState>> byToken_;
};
