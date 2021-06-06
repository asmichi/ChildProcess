// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Base.hpp"
#include "ExactBytesIO.hpp"
#include "Globals.hpp"
#include "MiscHelpers.hpp"
#include "Service.hpp"
#include "SocketHelpers.hpp"
#include <cstdio>
#include <cstring>
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

const int HelperHelloBytes = 4;
const unsigned char HelperHello[] = {0x41, 0x53, 0x4d, 0x43};
static_assert(sizeof(HelperHello) == HelperHelloBytes);

// The parent process will use System.Diagnostics.Process to create this helper process
// in order to avoid creating unmanged process in a .NET process.
// Otherwise the signal handler of CoreFX would 'steal' such an unmanaged process.
//
// Connect to the parent process from this child process because System.Diagnostics.Process does not let
// this process inherit fds from the parent process.
extern "C" int HelperMain(int argc, const char** argv)
{
    // Usage: AsmichiChildProcessHelper socket_path
    if (argc != 2)
    {
        PutFatalError("Invalid argc.");
        return 1;
    }

    const auto* path = argv[1];

    struct sockaddr_un addr;
    if (strlen(path) > sizeof(addr.sun_path) - 1)
    {
        PutFatalError("Socket path too long.");
        return 1;
    }

    // Connect to the parent.
    std::memset(&addr, 0, sizeof(addr));
    addr.sun_family = AF_UNIX;
    strcpy(addr.sun_path, path);

    auto maybeSock = CreateUnixStreamSocket();
    if (!maybeSock)
    {
        PutFatalError(errno, "socket");
        return 1;
    }

    int ret = connect(maybeSock->Get(), reinterpret_cast<struct sockaddr*>(&addr), sizeof(struct sockaddr_un));
    if (ret == -1)
    {
        PutFatalError(errno, "connect");
        return 1;
    }

    if (!SendExactBytes(maybeSock->Get(), HelperHello, HelperHelloBytes))
    {
        PutFatalError(errno, "send");
        return 1;
    }

    close(STDIN_FILENO);

    g_Service.Initialize(std::move(*maybeSock));
    const int exitCode = g_Service.Run();
    TRACE_INFO("Helper exiting: %d\n", exitCode);
    return exitCode;
}
