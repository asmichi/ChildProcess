// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "AncillaryDataSocket.hpp"
#include "ChildProcessState.hpp"
#include "UniqueResource.hpp"
#include <cstdint>
#include <pthread.h>
#include <signal.h>

enum class NotificationToService : std::uint8_t
{
    // SIGINT
    Interrupt,
    // SIGTERM
    Termination,
    // SIGQUIT
    Quit,
    // Request the service to reap children (SIGCHLD or "child process registered to g_ChildProcessStateMap")
    ReapRequest
};

class Service final
{
public:
    // Delayed initialization.
    void Initialize(int mainChannelFd);

    // Interface for subchannels.
    [[nodiscard]] bool NotifyChildRegistration();

    // Interface for the signal handler.
    void NotifySignal(int signum);

    // Interface for main.
    [[nodiscard]] int MainLoop(int mainChannelFd);

private:
    [[nodiscard]] bool WriteNotification(NotificationToService notification);
    [[nodiscard]] bool HandleNotificationPipeInput();
    [[nodiscard]] bool HandleReapRequest();
    [[nodiscard]] bool HandleMainChannelInput();
    [[nodiscard]] bool HandleMainChannelOutput();
    [[nodiscard]] bool NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo);

    // Write to wake up the service thread.
    int notificationPipeReadEnd_;
    int notificationPipeWriteEnd_;

    std::unique_ptr<AncillaryDataSocket> mainChannel_;
};
