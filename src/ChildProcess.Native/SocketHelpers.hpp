// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

// Socket helper functions.

#include "Base.hpp"
#include <cstddef>
#include <sys/socket.h>
#include <sys/types.h>

constexpr const int SocketMaxFdsPerCall = 3;

struct CmsgFds
{
    static const constexpr std::size_t BufferSize = CMSG_SPACE(sizeof(int) * SocketMaxFdsPerCall);
    alignas(cmsghdr) char Buffer[BufferSize];
};

[[nodiscard]] bool SendExactBytes(int fd, const void* buf, std::size_t len) noexcept;
[[nodiscard]] bool SendExactBytesWithFd(int fd, const void* buf, std::size_t len, const int* fds, std::size_t fdCount) noexcept;
[[nodiscard]] ssize_t SendWithFd(int fd, const void* buf, std::size_t len, const int* fds, std::size_t fdCount, BlockingFlag blocking) noexcept;
[[nodiscard]] bool RecvExactBytes(int fd, void* buf, std::size_t len) noexcept;

[[nodiscard]] constexpr int MakeSockFlags(BlockingFlag blocking) noexcept
{
    return (blocking == BlockingFlag::NonBlocking ? MSG_DONTWAIT : 0) | MSG_NOSIGNAL;
}

// true: successful (including EWOULDBLOCK)
// false: connection closed
[[nodiscard]] inline bool HandleSendError(BlockingFlag blocking, const char* str, int err) noexcept
{
    if (blocking == BlockingFlag::NonBlocking && IsWouldBlockError(err))
    {
        return true;
    }
    else if (IsConnectionClosedError(err))
    {
        return false;
    }
    else
    {
        FatalErrorAbort(err, str);
    }
}

inline void AbortIfFatalSendError(BlockingFlag blocking, const char* str, int err) noexcept
{
    (void)HandleSendError(blocking, str, err);
}

// true: successful (including EWOULDBLOCK)
// false: connection closed
[[nodiscard]] inline bool HandleSendResult(BlockingFlag blocking, const char* str, ssize_t bytesSent, int err) noexcept
{
    if (bytesSent > 0)
    {
        return true;
    }
    else if (bytesSent == 0)
    {
        // POSIX-conformant send will not return 0.
        FatalErrorAbort(errno, "send returned 0!");
    }
    else
    {
        return HandleSendError(blocking, str, err);
    }
}

// true: successful (including EWOULDBLOCK)
// false: connection closed
[[nodiscard]] inline bool HandleRecvError(BlockingFlag blocking, const char* str, int err) noexcept
{
    if (blocking == BlockingFlag::NonBlocking && IsWouldBlockError(err))
    {
        return true;
    }
    else if (IsConnectionClosedError(err))
    {
        return false;
    }
    else
    {
        FatalErrorAbort(err, str);
    }
}

inline void AbortIfFatalRecvError(BlockingFlag blocking, const char* str, int err) noexcept
{
    (void)HandleRecvError(blocking, str, err);
}

// true: successful (including EWOULDBLOCK)
// false: connection closed
[[nodiscard]] inline bool HandleRecvResult(BlockingFlag blocking, const char* str, ssize_t bytesReceived, int err) noexcept
{
    if (bytesReceived > 0)
    {
        return true;
    }
    else if (bytesReceived == 0)
    {
        return false;
    }
    else
    {
        return HandleRecvError(blocking, str, err);
    }
}
