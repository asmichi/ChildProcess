// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "SocketHelpers.hpp"
#include "Base.hpp"
#include "ExactBytesIO.hpp"
#include "MiscHelpers.hpp"
#include <array>
#include <cassert>
#include <cerrno>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <memory>
#include <optional>
#include <sys/socket.h>
#include <unistd.h>
#include <utility>

bool SendExactBytes(int fd, const void* buf, std::size_t len) noexcept
{
    auto f = [fd](const void* p, std::size_t partialLen) { return send_restarting(fd, p, partialLen, MakeSockFlags(BlockingFlag::Blocking)); };
    if (!WriteExactBytes(f, buf, len))
    {
        return HandleSendError(BlockingFlag::Blocking, "send", errno);
    }

    return true;
}

bool RecvExactBytes(int fd, void* buf, std::size_t len) noexcept
{
    auto f = [fd](void* p, std::size_t partialLen) { return recv_restarting(fd, p, partialLen, MakeSockFlags(BlockingFlag::Blocking)); };
    return ReadExactBytes(f, buf, len);
}

bool SendExactBytesWithFd(int fd, const void* buf, std::size_t len, const int* fds, std::size_t fdCount) noexcept
{
    if (fds == nullptr || fdCount == 0)
    {
        return SendExactBytes(fd, buf, len);
    }

    // Make sure to send fds only once.
    ssize_t bytesSent = SendWithFd(fd, buf, len, fds, fdCount, BlockingFlag::Blocking);
    if (!HandleSendResult(BlockingFlag::Blocking, "sendmsg", bytesSent, errno))
    {
        return false;
    }

    // Send out remaining bytes.
    std::size_t positiveBytesSent = static_cast<std::size_t>(bytesSent);
    if (positiveBytesSent >= len)
    {
        assert(positiveBytesSent == len);
        return true;
    }
    else
    {
        return SendExactBytes(fd, static_cast<const std::byte*>(buf) + positiveBytesSent, len - positiveBytesSent);
    }
}

ssize_t SendWithFd(int fd, const void* buf, std::size_t len, const int* fds, std::size_t fdCount, BlockingFlag blocking) noexcept
{
    if (fds == nullptr || fdCount == 0)
    {
        return send_restarting(fd, buf, len, MakeSockFlags(blocking));
    }

    if (fdCount > SocketMaxFdsPerCall)
    {
        errno = EINVAL;
        return -1;
    }

    iovec iov;
    msghdr msg;
    CmsgFds cmsgFds;

    iov.iov_base = const_cast<void*>(buf);
    iov.iov_len = len;
    msg.msg_name = nullptr;
    msg.msg_namelen = 0;
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;
    msg.msg_control = cmsgFds.Buffer;
    msg.msg_controllen = CmsgFds::BufferSize;
    msg.msg_flags = 0;

    struct cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg);
    pcmsghdr->cmsg_len = CMSG_LEN(sizeof(int) * fdCount);
    pcmsghdr->cmsg_level = SOL_SOCKET;
    pcmsghdr->cmsg_type = SCM_RIGHTS;
    std::memcpy(CMSG_DATA(pcmsghdr), fds, sizeof(int) * fdCount);

    return sendmsg_restarting(fd, &msg, 0);
}
