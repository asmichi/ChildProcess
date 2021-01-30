// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "ExactBytesIO.hpp"
#include "MiscHelpers.hpp"
#include "SocketHelpers.hpp"
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

namespace
{
    void EnqueueRemainingBytes(WriteBuffer& b, const void* buf, std::size_t len, ssize_t bytesSent, int err)
    {
        if (bytesSent > 0)
        {
            auto const positiveBytesSent = static_cast<size_t>(bytesSent);
            if (positiveBytesSent < len)
            {
                b.Enqueue(static_cast<const std::byte*>(buf) + positiveBytesSent, len - positiveBytesSent);
            }
        }
        else if (bytesSent == 0)
        {
            // POSIX-conformant send will not return 0.
            FatalErrorAbort(errno, "send returned 0!");
        }
        else
        {
            if (IsWouldBlockError(err))
            {
                b.Enqueue(static_cast<const std::byte*>(buf), len);
            }
        }
    }
} // namespace

AncillaryDataSocket::AncillaryDataSocket(UniqueFd&& sockFd) noexcept
    : fd_(std::move(sockFd))
{
}

AncillaryDataSocket::AncillaryDataSocket(int sockFd) noexcept
    : AncillaryDataSocket(UniqueFd(sockFd))
{
}

bool AncillaryDataSocket::SendBuffered(const void* buf, std::size_t len, BlockingFlag blocking) noexcept
{
    ssize_t bytesSent = send_restarting(fd_.Get(), buf, len, MakeSockFlags(blocking));
    int err = errno;
    EnqueueRemainingBytes(sendBuffer_, buf, len, bytesSent, err);
    errno = err;
    return HandleSendResult(blocking, "send", bytesSent, err);
}

bool AncillaryDataSocket::SendBufferedWithFd(const void* buf, std::size_t len, const int* fds, std::size_t fdCount, BlockingFlag blocking) noexcept
{
    ssize_t bytesSent = ::SendWithFd(fd_.Get(), buf, len, fds, fdCount, blocking);
    int err = errno;
    EnqueueRemainingBytes(sendBuffer_, buf, len, bytesSent, err);
    errno = err;
    return HandleSendResult(blocking, "sendmsg", bytesSent, err);
}

bool AncillaryDataSocket::SendExactBytes(const void* buf, std::size_t len) noexcept
{
    if (!Flush(BlockingFlag::Blocking))
    {
        return false;
    }

    return ::SendExactBytes(fd_.Get(), buf, len);
}

bool AncillaryDataSocket::SendExactBytesWithFd(const void* buf, std::size_t len, const int* fds, std::size_t fdCount) noexcept
{
    if (!Flush(BlockingFlag::Blocking))
    {
        return false;
    }

    return ::SendExactBytesWithFd(fd_.Get(), buf, len, fds, fdCount);
}

// Send data in sendBuffer_ until all data is sent or EWOULDBLOCK is returned.
bool AncillaryDataSocket::Flush(BlockingFlag blocking) noexcept
{
    while (sendBuffer_.HasPendingData())
    {
        std::byte* p;
        std::size_t len;
        std::tie(p, len) = sendBuffer_.GetPendingData();

        const ssize_t bytesSent = send_restarting(fd_.Get(), p, len, MakeSockFlags(blocking));
        if (!HandleSendResult(blocking, "send", bytesSent, errno))
        {
            return false;
        }

        sendBuffer_.Dequeue(static_cast<std::size_t>(bytesSent));
    }

    return true;
}

bool AncillaryDataSocket::RecvExactBytes(void* buf, std::size_t len) noexcept
{
    auto f = [this](void* p, std::size_t partialLen) { return Recv(p, partialLen, BlockingFlag::Blocking); };
    return ReadExactBytes(f, buf, len);
}

ssize_t AncillaryDataSocket::Recv(void* buf, std::size_t len, BlockingFlag blocking) noexcept
{
    iovec iov;
    msghdr msg;
    CmsgFds cmsgFds;

    iov.iov_base = buf;
    iov.iov_len = len;
    msg.msg_name = nullptr;
    msg.msg_namelen = 0;
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;
    msg.msg_control = cmsgFds.Buffer;
    msg.msg_controllen = CmsgFds::BufferSize;
    msg.msg_flags = 0;

    const ssize_t receivedBytes = recvmsg_restarting(fd_.Get(), &msg, MakeSockFlags(blocking) | MSG_CMSG_CLOEXEC);
    if (receivedBytes == -1)
    {
        return -1;
    }

    // Store received fds.
    bool shouldShutdown = false;

    for (cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg); pcmsghdr != nullptr; pcmsghdr = CMSG_NXTHDR(&msg, pcmsghdr))
    {
        if (pcmsghdr->cmsg_level != SOL_SOCKET || pcmsghdr->cmsg_type != SCM_RIGHTS)
        {
            // Logic error: The counterpart has a bug or we are connected with an untrusted counterpart.
            TRACE_FATAL("Received unknown cmsg (cmsg_level: %d, cmsg_type: %d). Shutting down the connection.\n", pcmsghdr->cmsg_level, pcmsghdr->cmsg_type);
            shouldShutdown = true;
            // Continue to read so that we will not leak received fds.
            continue;
        }

        unsigned char* const cmsgdata = CMSG_DATA(pcmsghdr);
        const std::ptrdiff_t cmsgdataLen = pcmsghdr->cmsg_len - (cmsgdata - reinterpret_cast<unsigned char*>(pcmsghdr));
        const std::size_t fdCount = cmsgdataLen / sizeof(int);
        for (std::size_t i = 0; i < fdCount; i++)
        {
            int receivedFd;
            std::memcpy(&receivedFd, cmsgdata + sizeof(int) * i, sizeof(int));
            receivedFds_.push(UniqueFd(receivedFd));
        }
    }

    if (shouldShutdown)
    {
        shutdown(fd_.Get(), SHUT_RDWR);
        errno = ECONNRESET;
        return -1;
    }

    return receivedBytes;
}
