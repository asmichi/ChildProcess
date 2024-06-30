// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "Base.hpp"
#include "UniqueResource.hpp"
#include "WriteBuffer.hpp"
#include <cstddef>
#include <optional>
#include <queue>
#include <utility>

// Receives fds via ancillary data on a unix domain socket. Employs a send buffer if nonblocking.
class AncillaryDataSocket final
{
public:
    static const constexpr int MaxFdsPerCall = 3;

    AncillaryDataSocket(UniqueFd&& sockFd, int cancellationPipeReadEnd) noexcept;

    [[nodiscard]] ssize_t Send(const void* buf, std::size_t len, BlockingFlag blocking) noexcept;
    [[nodiscard]] bool SendExactBytes(const void* buf, std::size_t len) noexcept;
    [[nodiscard]] bool SendBuffered(const void* buf, std::size_t len, BlockingFlag blocking) noexcept;
    [[nodiscard]] bool Flush(BlockingFlag blocking) noexcept;
    [[nodiscard]] bool HasPendingData() noexcept { return sendBuffer_.HasPendingData(); }

    [[nodiscard]] ssize_t Recv(void* buf, std::size_t len, BlockingFlag blocking) noexcept;
    [[nodiscard]] bool RecvExactBytes(void* buf, std::size_t len) noexcept;

    void Shutdown() noexcept;

    [[nodiscard]] int GetFd() const noexcept { return fd_.Get(); }

    [[nodiscard]] std::size_t ReceivedFdCount() const noexcept { return receivedFds_.size(); }

    [[nodiscard]] std::optional<UniqueFd> PopReceivedFd() noexcept
    {
        if (receivedFds_.size() == 0)
        {
            return std::nullopt;
        }

        UniqueFd fd{std::move(receivedFds_.front())};
        receivedFds_.pop();
        return fd;
    }

private:
    // true: won't block; false: cancellation requested.
    bool PollForInput();
    // true: won't block; false: cancellation requested.
    bool PollForOutput();
    bool PollFor(short event);

    UniqueFd fd_;
    // Close the counterpart to cancel current and future blocking operations.
    int cancellationPipeReadEnd_;
    std::queue<UniqueFd> receivedFds_;
    WriteBuffer sendBuffer_;
};
