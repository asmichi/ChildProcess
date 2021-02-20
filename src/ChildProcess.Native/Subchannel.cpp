// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Subchannel.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "BinaryReader.hpp"
#include "ChildProcessState.hpp"
#include "ErrorCodeExceptions.hpp"
#include "Globals.hpp"
#include "MiscHelpers.hpp"
#include "Request.hpp"
#include "Service.hpp"
#include "UniqueResource.hpp"
#include <algorithm>
#include <cassert>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <memory>
#include <poll.h>
#include <unistd.h>
#include <vector>

// After StartCommunicationThread succeeds, this instance must not be manipulated outside the communication thread.
bool Subchannel::StartCommunicationThread()
{
    auto maybeThread = CreateThreadWithMyDefault(Subchannel::CommunicationThreadFunc, reinterpret_cast<void*>(this), CreateThreadFlagsDetached);
    if (!maybeThread)
    {
        const std::int32_t err = errno;
        perror("pthread_create");
        TRACE_ERROR("Failed to create a subchannel communication thread on %d.", sock_.GetFd());
        static_cast<void>(WriteExactBytes(sock_.GetFd(), &err, sizeof(err)));
        return false;
    }

    return true;
}

void* Subchannel::CommunicationThreadFunc(void* arg)
{
    auto const pSubchannel = static_cast<Subchannel*>(arg);

    try
    {
        pSubchannel->CommunicationLoop();
    }
    catch ([[maybe_unused]] const CommunicationError& exn)
    {
        // NOTE: Orderly shutdown (errno=0) also reaches here.
        TRACE_INFO("Subchannel %d disconnected: %d\n", pSubchannel->sock_.GetFd(), exn.GetError());
    }

    g_Service.NotifySubchannelClosed(pSubchannel);

    return nullptr;
}

void Subchannel::CommunicationLoop()
{
    std::int32_t err = 0;

    // Report successful creation.
    if (!WriteExactBytes(sock_.GetFd(), &err, sizeof(err)))
    {
        return;
    }

    while (true)
    {
        try
        {
            RawRequest rawRequest;
            RecvRawRequest(&rawRequest);

            switch (rawRequest.Command)
            {
            case RequestCommand::SpawnProcess:
                HandleProcessCreationCommand(std::move(rawRequest.Body), rawRequest.BodyLength);
                break;

            case RequestCommand::SendSignal:
                HandleSendSignalCommand(std::move(rawRequest.Body), rawRequest.BodyLength);
                break;

            default:
                TRACE_ERROR("Unknown command: %u\n", static_cast<std::uint32_t>(rawRequest.Command));
                static_cast<void>(SendError(ErrorCode::InvalidRequest));
                break;
            }
        }
        catch (const BadRequestError& exn)
        {
            static_cast<void>(SendError(exn.GetError()));
        }
    }
}

void Subchannel::HandleProcessCreationCommand(std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength)
{
    SpawnProcessRequest r;
    ToProcessCreationRequest(&r, std::move(body), bodyLength);
    HandleProcessCreationRequest(r);
}

void Subchannel::ToProcessCreationRequest(SpawnProcessRequest* r, std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength)
{
    DeserializeSpawnProcessRequest(r, std::move(body), bodyLength);

    auto popOrThrow = [this] {
        auto maybeFd = sock_.PopReceivedFd();
        if (!maybeFd)
        {
            TRACE_ERROR("Insufficient fds in a request.\n");
            throw BadRequestError(ErrorCode::InvalidRequest);
        }
        return std::move(*maybeFd);
    };

    if (r->Flags & RequestFlagsRedirectStdin)
    {
        r->StdinFd = popOrThrow();
    }
    if (r->Flags & RequestFlagsRedirectStdout)
    {
        r->StdoutFd = popOrThrow();
    }
    if (r->Flags & RequestFlagsRedirectStderr)
    {
        r->StderrFd = popOrThrow();
    }
    if (sock_.ReceivedFdCount() != 0)
    {
        TRACE_ERROR("Too many fds in a request. Flags=%x, %zu fds remaining.\n", r->Flags, sock_.ReceivedFdCount());
        throw BadRequestError(ErrorCode::InvalidRequest);
    }
}

void Subchannel::HandleProcessCreationRequest(const SpawnProcessRequest& r)
{
    auto maybeOutPipe = CreatePipe();
    if (!maybeOutPipe)
    {
        SendResponse(errno, 0);
        return;
    }
    auto maybeInPipe = CreatePipe();
    if (!maybeInPipe)
    {
        SendResponse(errno, 0);
        return;
    }

    // NOTE: These fds may be inherited by multiple forked processes.
    // parent -> child : To signal "the parent is ready; perform exec"
    auto outPipe = std::move(*maybeOutPipe);
    // child -> parent : To signal exec error (or no write on success)
    auto inPipe = std::move(*maybeInPipe);

    int childPid = fork();
    if (childPid == -1)
    {
        SendResponse(errno, 0);
    }
    else if (childPid == 0)
    {
        // child
        outPipe.WriteEnd.Reset();
        inPipe.ReadEnd.Reset();

        auto dup2OrFail = [](const UniqueFd& writeEnd, const UniqueFd& src, int dst) {
            if (src.IsValid())
            {
                if (dup2(src.Get(), dst) == -1)
                {
                    int err = errno;
                    static_cast<void>(WriteExactBytes(writeEnd.Get(), &err, sizeof(err)));
                    _exit(1);
                }
            }
        };

        auto reportError = [](int fd, int err) {
            static_cast<void>(WriteExactBytes(fd, &err, sizeof(err)));
        };

        dup2OrFail(inPipe.WriteEnd, r.StdinFd, STDIN_FILENO);
        dup2OrFail(inPipe.WriteEnd, r.StdoutFd, STDOUT_FILENO);
        dup2OrFail(inPipe.WriteEnd, r.StderrFd, STDERR_FILENO);

        if (r.WorkingDirectory != nullptr)
        {
            if (chdir_restarting(r.WorkingDirectory) == -1)
            {
                reportError(inPipe.WriteEnd.Get(), errno);
                _exit(1);
            }
        }

        // Wait for the parent to be ready
        char c;
        if (!ReadExactBytes(outPipe.ReadEnd.Get(), &c, 1))
        {
            // The parent has been killed; no point in continuing.
            _exit(1);
        }

        // Always create a new process group.
        setpgid(0, 0);
        // NOTE: POSIX specifies execve shall not modify argv and envp.
        execve(r.ExecutablePath, const_cast<char* const*>(&r.Argv[0]), const_cast<char* const*>(&r.Envp[0]));

        reportError(inPipe.WriteEnd.Get(), errno);
        _exit(1);
    }
    else
    {
        // parent
        outPipe.ReadEnd.Reset();
        inPipe.WriteEnd.Reset();

        // Register the child before the child performs exec.
        g_ChildProcessStateMap.Allocate(childPid, r.Token);

        // Send a reap request in case the child has already been killed and we have delayed reaping.
        if (!g_Service.NotifyChildRegistration())
        {
            FatalErrorAbort(errno, "write");
        }

        // Make the child to perform exec.
        if (!WriteExactBytes(outPipe.WriteEnd.Get(), "", 1))
        {
            // The child has already been killed.
            SendResponse(errno, 0);
            return;
        }

        int err = 0;
        const bool execSuccessful = !ReadExactBytes(inPipe.ReadEnd.Get(), &err, sizeof(err));
        if (execSuccessful)
        {
            SendResponse(0, childPid);
        }
        else
        {
            // Failed to execute the program: failed to dup2 or execve.
            SendResponse(err, 0);
        }
    }
}

void Subchannel::HandleSendSignalCommand(std::unique_ptr<std::byte[]> body, std::uint32_t bodyLength)
{
    SendSignalRequest r;
    DeserializeSendSignalRequest(&r, std::move(body), bodyLength);

    auto nativeSignal = ToNativeSignal(r.Signal);
    if (!nativeSignal)
    {
        throw BadRequestError(ErrorCode::InvalidRequest);
    }

    auto pState = g_ChildProcessStateMap.GetByToken(r.Token);
    if (!pState)
    {
        // The process has already been reaped.
        SendSuccess(0);
    }
    else if (pState->SendSignal(nativeSignal.value()))
    {
        if (r.Signal == AbstractSignal::Termination)
        {
            // Also send SIGCONT to ensure termination.
            static_cast<void>(pState->SendSignal(SIGCONT));
        }

        // Sent a signal.
        SendSuccess(0);
    }
    else if (errno == ESRCH)
    {
        // The process has already been reaped.
        SendSuccess(0);
    }
    else
    {
        SendError(errno);
    }
}

std::optional<int> Subchannel::ToNativeSignal(AbstractSignal abstractSignal) noexcept
{
    switch (abstractSignal)
    {
    case AbstractSignal::Interrupt:
        return SIGINT;

    case AbstractSignal::Kill:
        return SIGKILL;

    case AbstractSignal::Termination:
        return SIGTERM;

    default:
        return std::nullopt;
    }
}

void Subchannel::RecvRawRequest(RawRequest* r)
{
    std::uint32_t commandAndLength[2];
    if (!sock_.RecvExactBytes(&commandAndLength, sizeof(commandAndLength)))
    {
        // Throws even for a normal shutdown (errno = 0).
        throw CommunicationError(errno);
    }

    const RequestCommand command = static_cast<RequestCommand>(commandAndLength[0]);
    const std::uint32_t bodyLength = commandAndLength[1];

    if (bodyLength > MaxReqeuestLength)
    {
        TRACE_ERROR("Request too big: %u\n", static_cast<unsigned int>(bodyLength));

        // Discard the request body.
        const size_t BufSize = 64 * 1024;
        auto buf = std::make_unique<std::byte[]>(BufSize);
        size_t totalReceivedBytes = 0;
        while (totalReceivedBytes < bodyLength)
        {
            std::size_t bytesToReceive = std::min(BufSize, bodyLength - totalReceivedBytes);
            ssize_t receivedBytes = sock_.Recv(buf.get(), bytesToReceive, BlockingFlag::Blocking);
            if (receivedBytes <= 0)
            {
                throw new CommunicationError(errno);
            }
            totalReceivedBytes += receivedBytes;
        }

        throw BadRequestError(E2BIG);
    }

    auto body = std::make_unique<std::byte[]>(bodyLength);
    if (!sock_.RecvExactBytes(&body[0], bodyLength))
    {
        // Throws even for a normal shutdown (errno = 0).
        throw CommunicationError(errno);
    }

    r->BodyLength = bodyLength;
    r->Body = std::move(body);
    r->Command = command;
}

void Subchannel::SendSuccess(std::int32_t data)
{
    SendResponse(0, data);
}

void Subchannel::SendError(int err)
{
    SendResponse(err, 0);
}

void Subchannel::SendResponse(int err, std::int32_t data)
{
    static_assert(sizeof(int) == 4);

    std::byte buf[8];
    std::memcpy(&buf[0], &err, 4);
    std::memcpy(&buf[4], &data, 4);
    if (!sock_.SendExactBytes(buf, 8))
    {
        throw CommunicationError(errno);
    }
}
