// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "AncillaryDataSocket.hpp"
#include "ChildProcessState.hpp"
#include "UniqueResource.hpp"
#include <cstdint>
#include <pthread.h>
#include <signal.h>

struct ChildExitNotification
{
    uint64_t Token;
    // ProcessID
    int32_t ProcessID;
    // Exit status on CLD_EXITED; -N on CLD_KILLED and CLD_DUMPED where N is the signal number.
    int32_t Status;
};
static_assert(sizeof(ChildExitNotification) == 16);

class Service final
{
public:
    // Delayed initialization.
    void Initialize(int mainChannelFd);

    // Interface for main.
    [[nodiscard]] int MainLoop(int mainChannelFd);

    // Interface for subchannels.
    [[nodiscard]] bool NotifyChildRegistration();

    // Interface for the signal handler.
    void NotifySignal(int signum);

private:
    [[nodiscard]] bool HandleSignalDataPipeInput();
    [[nodiscard]] bool HandleReapRequestPipeInput();
    [[nodiscard]] bool HandleReapRequest();
    [[nodiscard]] bool HandleMainChannelInput();
    [[nodiscard]] bool HandleMainChannelOutput();
    [[nodiscard]] bool NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo);

    // The signal handler writes signal numbers here (except SIGCHLD, which will be written to reapRequestPipeWriteEnd_).
    int signalDataPipeReadEnd_;
    int signalDataPipeWriteEnd_;

    // Subchannels will write a dummy byte here to request the service to reap children. SIGCHLD will also be written here.
    int reapRequestPipeReadEnd_;
    int reapRequestPipeWriteEnd_;

    std::unique_ptr<AncillaryDataSocket> mainChannel_;
};
