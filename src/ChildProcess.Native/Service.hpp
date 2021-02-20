// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "AncillaryDataSocket.hpp"
#include "ChildProcessState.hpp"
#include "SubchannelCollection.hpp"
#include "UniqueResource.hpp"
#include <cstdint>
#include <memory>
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
    ReapRequest,
    // A subchannel is closed, indicating that we may be able to exit.
    SubchannelClosed,
};

class Service final
{
public:
    // Interface for main.
    // Delayed initialization.
    void Initialize(UniqueFd mainChannelFd);
    [[nodiscard]] int Run();

    // Interface for subchannels.
    void NotifyChildRegistration();
    void NotifySubchannelClosed(Subchannel* pSubchannel);

    // Interface for the signal handler.
    void NotifySignal(int signum);

private:
    [[nodiscard]] bool WriteNotification(NotificationToService notification);
    void InitiateShutdown();
    bool ShouldExit();
    void HandleNotificationPipeInput();
    void ReapAllExitedChildren();
    void HandleMainChannelInput();
    void HandleMainChannelOutput();
    void NotifyClientOfExitedChild(ChildProcessState* pState, siginfo_t siginfo);

    bool shuttingDown_ = false;

    // Write to wake up the service thread.
    int notificationPipeReadEnd_ = 0;
    int notificationPipeWriteEnd_ = 0;

    // Close the write end to cancel all current and future blocking send/recv.
    int cancellationPipeWriteEnd_ = 0;
    int cancellationPipeReadEnd_ = 0;

    SubchannelCollection subchannelCollection_;
    std::unique_ptr<AncillaryDataSocket> mainChannel_;
};
