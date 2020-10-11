// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "MiscHelpers.hpp"
#include "Base.hpp"
#include "ExactBytesIO.hpp"
#include "UniqueResource.hpp"
#include <array>
#include <cerrno>
#include <cstddef>
#include <fcntl.h>
#include <optional>
#include <poll.h>
#include <sys/socket.h>
#include <unistd.h>

namespace
{
    template<typename Func>
    auto InvokeRestarting(Func f) noexcept
    {
        decltype(f()) ret;
        do
        {
            ret = f();
        } while (ret < 0 && errno == EINTR);
        return ret;
    }
} // namespace

ssize_t recv_restarting(int fd, void* buf, size_t len, int flags) noexcept
{
    return InvokeRestarting(
        [=] { return recv(fd, buf, len, flags); });
}

ssize_t recvmsg_restarting(int fd, struct msghdr* msg, int flags) noexcept
{
    return InvokeRestarting(
        [=] { return recvmsg(fd, msg, flags); });
}

ssize_t send_restarting(int fd, const void* buf, size_t len, int flags) noexcept
{
    return InvokeRestarting(
        [=] { return send(fd, buf, len, flags); });
}

ssize_t sendmsg_restarting(int fd, const struct msghdr* msg, int flags) noexcept
{
    return InvokeRestarting(
        [=] { return sendmsg(fd, msg, flags); });
}

ssize_t read_restarting(int fd, void* buf, size_t len) noexcept
{
    return InvokeRestarting(
        [=] { return read(fd, buf, len); });
}

ssize_t write_restarting(int fd, const void* buf, size_t len) noexcept
{
    return InvokeRestarting(
        [=] { return write(fd, buf, len); });
}

bool ReadExactBytes(int fd, void* buf, std::size_t len) noexcept
{
    auto f = [fd](void* p, std::size_t partialLen) { return read_restarting(fd, p, partialLen); };
    return ReadExactBytes(f, buf, len);
}

bool WriteExactBytes(int fd, const void* buf, std::size_t len) noexcept
{
    auto f = [fd](const void* p, std::size_t partialLen) { return write_restarting(fd, p, partialLen); };
    return WriteExactBytes(f, buf, len);
}

int poll_restarting(struct pollfd* fds, unsigned int nfds, int timeout) noexcept
{
    int ret;
    do
    {
        ret = poll(fds, nfds, timeout);
    } while (ret < 0 && (errno == EINTR || errno == EAGAIN));
    return ret;
}

int chdir_restarting(const char* path) noexcept
{
    int ret;
    do
    {
        ret = chdir(path);
    } while (ret < 0 && (errno == EINTR || errno == EAGAIN));
    return ret;
}

std::optional<PipeEnds> CreatePipe() noexcept
{
    int pipes[2];
    if (pipe2(pipes, O_CLOEXEC) != 0)
    {
        return std::nullopt;
    }

    PipeEnds ret;
    ret.ReadEnd = UniqueFd(pipes[0]);
    ret.WriteEnd = UniqueFd(pipes[1]);
    return std::move(ret);
}

std::optional<std::array<UniqueFd, 2>> CreateUnixStreamSocketPair() noexcept
{
    int socks[2];
    if (socketpair(AF_UNIX, SOCK_STREAM | SOCK_CLOEXEC, 0, socks) != 0)
    {
        return std::nullopt;
    }

    return std::array<UniqueFd, 2>{UniqueFd(socks[0]), UniqueFd(socks[1])};
}

std::optional<pthread_t> CreateThreadWithMyDefault(void* (*startRoutine)(void*), void* arg, int flags) noexcept
{
    // glibc default
    const std::size_t requestedStackSize = 2 * 1024 * 1024;
    // Round up to the page boundary in case the system employs super huge pages (not seen today, though).
    const std::size_t stackSize = std::max<size_t>(requestedStackSize, sysconf(_SC_PAGESIZE));

    pthread_attr_t attr;
    pthread_attr_init(&attr);
    pthread_attr_setstacksize(&attr, stackSize);
    if (flags & CreateThreadFlagsDetached)
    {
        pthread_attr_setdetachstate(&attr, PTHREAD_CREATE_DETACHED);
    }

    pthread_t threadId;
    int err = pthread_create(&threadId, &attr, startRoutine, arg);
    pthread_attr_destroy(&attr);

    if (err != 0)
    {
        errno = err;
        return std::nullopt;
    }

    return threadId;
}
