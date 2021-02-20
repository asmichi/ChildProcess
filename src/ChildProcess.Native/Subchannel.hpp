// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "AncillaryDataSocket.hpp"
#include "Request.hpp"
#include "UniqueResource.hpp"
#include <cstdint>
#include <optional>

const std::uint32_t MaxReqeuestLength = 2 * 1024 * 1024;

struct RawRequest final
{
    RequestCommand Command;
    uint32_t BodyLength;
    std::unique_ptr<std::byte[]> Body;
};

class Subchannel final
{
public:
    explicit Subchannel(UniqueFd sockFd) noexcept : sock_(std::move(sockFd)) {}

    [[nodiscard]] bool StartCommunicationThread();

private:
    static void* CommunicationThreadFunc(void* arg);
    void CommunicationLoop();

    void HandleProcessCreationCommand(std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength);
    void ToProcessCreationRequest(SpawnProcessRequest* r, std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength);
    void HandleProcessCreationRequest(const SpawnProcessRequest& r);

    void HandleSendSignalCommand(std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength);
    std::optional<int> ToNativeSignal(AbstractSignal abstractSignal) noexcept;

    void RecvRawRequest(RawRequest* r);
    void SendSuccess(std::int32_t data);
    void SendError(int err);
    void SendResponse(int err, std::int32_t data);

    AncillaryDataSocket sock_;
};
