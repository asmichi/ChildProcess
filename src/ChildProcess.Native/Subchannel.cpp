// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Subchannel.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "BinaryReader.hpp"
#include "ErrnoExceptions.hpp"
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
#include <unistd.h>
#include <vector>

class Subchannel final
{
public:
    explicit Subchannel(UniqueFd sockFd) noexcept : sock_(std::move(sockFd)) {}

    static void StartHandler(UniqueFd sockFd);

private:
    static void* ThreadFunc(void* arg);
    void MainLoop();
    void ReadRequest(Request* r);
    void HandleRequest(const Request& r);
    void SendSuccess(std::uint32_t pid);
    void SendError(int err);
    void SendResponse(int err, std::uint32_t pid);

    AncillaryDataSocket sock_;
};

void StartSubchannelHandler(UniqueFd sockFd)
{
    Subchannel::StartHandler(std::move(sockFd));
}

void Subchannel::StartHandler(UniqueFd sockFd)
{
    auto maybeThread = CreateThreadWithMyDefault(Subchannel::ThreadFunc, reinterpret_cast<void*>(sockFd.Get()), CreateThreadFlagsDetached);
    if (!maybeThread)
    {
        const std::int32_t err = errno;
        perror("pthread_create");
        static_cast<void>(WriteExactBytes(sockFd.Get(), &err, sizeof(err)));
        return;
    }

    // At this point, the thread owns the ownership of the fd.
    static_cast<void>(sockFd.Release());
}

void* Subchannel::ThreadFunc(void* arg)
{
    const int sockFd = static_cast<int>(reinterpret_cast<uintptr_t>(arg));
    Subchannel subchannel{UniqueFd(sockFd)};
    try
    {
        subchannel.MainLoop();
    }
    catch ([[maybe_unused]] const CommunicationError& exn)
    {
        TRACE_INFO("Subchannel %d disconnected: %d\n", sockFd, exn.GetError());
    }
    return nullptr;
}

void Subchannel::MainLoop()
{
    std::int32_t err = 0;

    // Report successful creation.
    if (!WriteExactBytes(sock_.GetFd(), &err, sizeof(err)))
    {
        return;
    }

    while (true)
    {
        Request r;
        try
        {
            ReadRequest(&r);
        }
        catch (const BadRequestError& exn)
        {
            // TODO: More explicitly distinguish BadRequest (logic error) from other errors.
            static_cast<void>(SendError(-exn.GetError()));
            return;
        }

        HandleRequest(r);
    }
}

void Subchannel::ReadRequest(Request* r)
{
    std::uint32_t length;
    if (!sock_.RecvExactBytes(&length, sizeof(length)))
    {
        // Throws even for a normal shutdown (errno = 0).
        throw CommunicationError(errno);
    }

    if (length > MaxReqeuestLength)
    {
        TRACE_ERROR("Request too big: %u\n", static_cast<unsigned int>(length));
        throw BadRequestError(E2BIG);
    }

    auto buf = std::make_unique<std::byte[]>(length);
    if (!sock_.RecvExactBytes(&buf[0], length))
    {
        // Throws even for a normal shutdown (errno = 0).
        throw CommunicationError(errno);
    }

    DeserializeRequest(r, std::move(buf), length);

    auto popOrThrow = [this] {
        auto maybeFd = sock_.PopReceivedFd();
        if (!maybeFd)
        {
            TRACE_ERROR("Insufficient fds in a request.\n");
            throw BadRequestError(EINVAL);
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
        TRACE_ERROR("Too many fds in a request.\n");
        throw BadRequestError(EINVAL);
    }
}

void Subchannel::HandleRequest(const Request& r)
{
    auto maybePipe = CreatePipe();
    if (!maybePipe)
    {
        SendResponse(errno, 0);
        return;
    }

    auto pipe = std::move(*maybePipe);

    int childPid = fork();
    if (childPid == -1)
    {
        SendResponse(errno, 0);
    }
    else if (childPid == 0)
    {
        // child
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

        dup2OrFail(pipe.WriteEnd, r.StdinFd, STDIN_FILENO);
        dup2OrFail(pipe.WriteEnd, r.StdoutFd, STDOUT_FILENO);
        dup2OrFail(pipe.WriteEnd, r.StderrFd, STDERR_FILENO);

        if (r.WorkingDirectory != nullptr)
        {
            if (chdir_restarting(r.WorkingDirectory) == -1)
            {
                reportError(pipe.WriteEnd.Get(), errno);
                _exit(1);
            }
        }

        // NOTE: POSIX specifies execve shall not modify argv and envp.
        execve(r.ExecutablePath, const_cast<char* const*>(&r.Argv[0]), const_cast<char* const*>(&r.Envp[0]));

        reportError(pipe.WriteEnd.Get(), errno);
        _exit(1);
    }
    else
    {
        // parent
        pipe.WriteEnd.Reset();

        int err = 0;
        const bool execSuccessful = !ReadExactBytes(pipe.ReadEnd.Get(), &err, sizeof(err));
        if (execSuccessful)
        {
            SendResponse(0, childPid);
        }
        else
        {
            // Failed to execute the program: failed to dup2 or execve.
            SendResponse(err, 0);
        }

        if (!WriteToChildCreationPipe(childPid, r.Token))
        {
            FatalErrorAbort(errno, "write");
        }
    }
}

void Subchannel::SendSuccess(std::uint32_t pid)
{
    SendResponse(0, pid);
}

void Subchannel::SendError(int err)
{
    SendResponse(err, 0);
}

void Subchannel::SendResponse(int err, std::uint32_t pid)
{
    static_assert(sizeof(int) == 4);

    std::byte buf[8];
    std::memcpy(&buf[0], &err, 4);
    std::memcpy(&buf[4], &pid, 4);
    if (!sock_.SendExactBytes(buf, 8))
    {
        throw CommunicationError(errno);
    }
}
