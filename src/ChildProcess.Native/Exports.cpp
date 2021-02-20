// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

// Interface for the managed implementation.

#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "MiscHelpers.hpp"
#include "Request.hpp"
#include "Service.hpp"
#include "SocketHelpers.hpp"
#include "UniqueResource.hpp"
#include <cassert>
#include <cstdio>
#include <cstring>
#include <dlfcn.h>
#include <fcntl.h>
#include <memory>
#include <stdexcept>
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

static_assert(sizeof(int) == 4);

namespace
{
    enum FileAccess
    {
        FileAccessRead = 1,
        FileAccessWrite = 2,
    };

    [[nodiscard]] bool IsWithinFdRange(std::intptr_t fd) noexcept
    {
        return 0 <= fd && fd <= std::numeric_limits<int>::max();
    }
} // namespace

extern "C" bool ConnectToUnixSocket(const char* path, intptr_t* outSock)
{
    struct sockaddr_un name;
    if (std::strlen(path) > sizeof(name.sun_path) - 1)
    {
        errno = ENAMETOOLONG;
        return false;
    }

    int sock = socket(AF_UNIX, SOCK_STREAM | SOCK_CLOEXEC, 0);
    if (sock == -1)
    {
        return false;
    }

    std::memset(&name, 0, sizeof(name));
    name.sun_family = AF_UNIX;
    std::strcpy(name.sun_path, path);

    if (connect(sock, reinterpret_cast<struct sockaddr*>(&name), sizeof(name)) == -1)
    {
        close(sock);
        return false;
    }

    *outSock = sock;
    return true;
}

extern "C" bool CreatePipe(intptr_t* readEnd, intptr_t* writeEnd)
{
    auto maybePipe = CreatePipe();
    if (!maybePipe)
    {
        return false;
    }

    *readEnd = maybePipe->ReadEnd.Release();
    *writeEnd = maybePipe->WriteEnd.Release();

    return true;
}

extern "C" bool CreateUnixStreamSocketPair(intptr_t* sock1, intptr_t* sock2)
{
    auto maybeSockerPair = CreateUnixStreamSocketPair();
    if (!maybeSockerPair)
    {
        return -1;
    }

    *sock1 = (*maybeSockerPair)[0].Release();
    *sock2 = (*maybeSockerPair)[1].Release();

    return true;
}

// Duplicate the std* handle of the current process if it will not cause SIGTTIN/SIGTTOU in the child process.
extern "C" bool DuplicateStdFileForChild(int stdFd, bool createNewProcessGroup, intptr_t* outFd)
{
    if (stdFd != STDIN_FILENO && stdFd != STDOUT_FILENO && stdFd != STDERR_FILENO)
    {
        errno = EINVAL;
        return false;
    }

    // First duplicate the fd since we do not own it and it can be replaced with another file description.
    auto newFd = DuplicateFd(stdFd);
    if (!newFd)
    {
        assert(errno == EBADF);
        *outFd = -1;
        return true;
    }

    if (!createNewProcessGroup)
    {
        // If the child process will be created within the current process group, this fd will not cause SIGTTIN/SIGTTOU.
        *outFd = newFd->Release();
        return true;
    }

    if (isatty(newFd->Get()))
    {
        // If the fd refers to a terminal, it will cause SIGTTIN/SIGTTOU if passed to another process group.
        *outFd = -1;
        return false;
    }
    else
    {
        assert(errno != EBADF);
        *outFd = newFd->Release();
        return true;
    }
}

// Writes the path of this DLL to buf if buf has sufficient space.
// On success, returns the length of the path (including NUL).
// On error, returns -1.
extern "C" std::int32_t GetDllPath(char* buf, std::int32_t len)
{
#if defined(__linux__)
    Dl_info info;
    if (!dladdr(reinterpret_cast<const void*>(&GetDllPath), &info))
    {
        return -1;
    }

    const size_t fnameLen = strlen(info.dli_fname);
    if (fnameLen > std::numeric_limits<std::int32_t>::max() - 1)
    {
        assert(false);
        errno = ENOMEM;
        return -1;
    }

    const std::int32_t requiredBufLen = static_cast<std::int32_t>(fnameLen) + 1;
    if (buf != nullptr && len >= requiredBufLen)
    {
        std::memcpy(buf, info.dli_fname, requiredBufLen);
    }

    return requiredBufLen;
#else
#error dladdr is linux-specific.
#endif
}

extern "C" int GetENOENT()
{
    return ENOENT;
}

// Returns maximum number of bytes of a unix domain socket path (excluding the terminating NUL byte).
extern "C" std::size_t GetMaxSocketPathLength()
{
    struct sockaddr_un addr;
    return sizeof(addr.sun_path) - 1;
}

extern "C" int GetPid()
{
    static_assert(sizeof(pid_t) == sizeof(int));
    return static_cast<int>(getpid());
}

// Opens "/dev/nul" with the specified mode.
extern "C" std::intptr_t OpenNullDevice(int fileAccess)
{
    mode_t mode;
    if (fileAccess & (FileAccessRead | FileAccessWrite))
    {
        mode = O_RDWR;
    }
    else if (fileAccess & FileAccessRead)
    {
        mode = O_RDONLY;
    }
    else if (fileAccess & FileAccessWrite)
    {
        mode = O_WRONLY;
    }
    else
    {
        errno = EINVAL;
        return -1;
    }

    return open("/dev/null", O_CLOEXEC, mode);
}

// Creates a subchannel.
// On success, returns the subchannel fd.
// On error, sets errno and returns -1.
extern "C" std::intptr_t SubchannelCreate(std::intptr_t mainChannelFd)
{
    if (!IsWithinFdRange(mainChannelFd))
    {
        errno = EINVAL;
        return -1;
    }

    // Create a subchannel socket pair.
    auto maybeSockerPair = CreateUnixStreamSocketPair();
    if (!maybeSockerPair)
    {
        return -1;
    }

    auto localSock = std::move((*maybeSockerPair)[0]);
    auto remoteSock = std::move((*maybeSockerPair)[1]);

    // Send remoteSock to the helper process and request subchannel creation.
    const int fds[1]{remoteSock.Get()};
    const char dummyData = 0;
    // We should not split this.
    if (!SendExactBytesWithFd(static_cast<int>(mainChannelFd), &dummyData, 1, fds, 1))
    {
        return -1;
    }

    remoteSock.Reset();

    // Receive the creation result.
    std::int32_t err;
    if (!RecvExactBytes(localSock.Get(), &err, sizeof(err)))
    {
        return -1;
    }

    if (err != 0)
    {
        errno = err;
        return -1;
    }

    return localSock.Release();
}

// Closes a subchannel.
extern "C" bool SubchannelDestroy(std::intptr_t subchannelFd)
{
    if (!IsWithinFdRange(subchannelFd))
    {
        errno = EINVAL;
        return -1;
    }

#if defined(__linux__)
    // On Linux, even when close returns EINTR, the fd has been closed.
    return close(static_cast<int>(subchannelFd)) == 0 || errno == EINTR;
#else
#error Not implemented.
#endif
}

// Receives data to entire buf
extern "C" bool SubchannelRecvExactBytes(std::intptr_t subchannelFd, void* buf, std::size_t len) noexcept
{
    if (!IsWithinFdRange(subchannelFd))
    {
        errno = EINVAL;
        return false;
    }

    return RecvExactBytes(static_cast<int>(subchannelFd), buf, len);
}

// Sends entire data
extern "C" bool SubchannelSendExactBytes(std::intptr_t subchannelFd, const void* buf, std::size_t len) noexcept
{
    if (!IsWithinFdRange(subchannelFd))
    {
        errno = EINVAL;
        return false;
    }

    return SendExactBytes(static_cast<int>(subchannelFd), buf, len);
}

// Sends entire data along with fds.
extern "C" bool SubchannelSendExactBytesAndFds(std::intptr_t subchannelFd, const void* buf, std::size_t len, const int* fds, std::size_t fdCount) noexcept
{
    if (!IsWithinFdRange(subchannelFd))
    {
        errno = EINVAL;
        return false;
    }

    return SendExactBytesWithFd(static_cast<int>(subchannelFd), buf, len, fds, fdCount);
}
