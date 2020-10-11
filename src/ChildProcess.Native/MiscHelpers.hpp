// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

// Miscellaneous helper functions.

#include "UniqueResource.hpp"
#include <array>
#include <optional>
#include <pthread.h>

struct pollfd;

// Wrappers that restarts the operation on EINTR.
[[nodiscard]] ssize_t recv_restarting(int fd, void* buf, size_t len, int flags) noexcept;
[[nodiscard]] ssize_t recvmsg_restarting(int fd, struct msghdr* msg, int flags) noexcept;
[[nodiscard]] ssize_t send_restarting(int fd, const void* buf, size_t len, int flags) noexcept;
[[nodiscard]] ssize_t sendmsg_restarting(int fd, const struct msghdr* msg, int flags) noexcept;
[[nodiscard]] ssize_t read_restarting(int fd, void* buf, size_t len) noexcept;
[[nodiscard]] ssize_t write_restarting(int fd, const void* buf, size_t len) noexcept;
[[nodiscard]] bool ReadExactBytes(int fd, void* buf, std::size_t len) noexcept;
[[nodiscard]] bool WriteExactBytes(int fd, const void* buf, std::size_t len) noexcept;
[[nodiscard]] int poll_restarting(struct pollfd* fds, unsigned int nfds, int timeout) noexcept;
[[nodiscard]] int chdir_restarting(const char* path) noexcept;

// RAII wrappers.
struct PipeEnds
{
    UniqueFd ReadEnd;
    UniqueFd WriteEnd;
};

[[nodiscard]] std::optional<PipeEnds> CreatePipe() noexcept;
[[nodiscard]] std::optional<std::array<UniqueFd, 2>> CreateUnixStreamSocketPair() noexcept;

// Wrappers with my default values.
enum CreateThreadFlags : int
{
    CreateThreadFlagsDetached = 1,
};

[[nodiscard]] std::optional<pthread_t> CreateThreadWithMyDefault(void* (*startRoutine)(void*), void* arg, int flags) noexcept;
