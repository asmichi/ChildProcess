// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "UniqueResource.hpp"
#include <cstddef>
#include <cstdint>
#include <memory>
#include <vector>

// Limitations to prevent OOM errors.
const std::uint32_t MaxMessageLength = 2 * 1024 * 1024;
const std::uint32_t MaxStringArrayCount = 64 * 1024;

enum RequestFlags
{
    RequestFlagsRedirectStdin = 1 << 0,
    RequestFlagsRedirectStdout = 1 << 1,
    RequestFlagsRedirectStderr = 1 << 2,
};

struct Request final
{
    std::unique_ptr<const std::byte[]> Data;
    std::uint64_t Token;
    std::uint32_t Flags;
    const char* WorkingDirectory;
    const char* ExecutablePath;
    std::vector<const char*> Argv;
    std::vector<const char*> Envp;
    UniqueFd StdinFd;
    UniqueFd StdoutFd;
    UniqueFd StderrFd;
};

// NOTE: DeserializeRequest does not set fds.
void DeserializeRequest(Request* r, std::unique_ptr<const std::byte[]> data, std::size_t length);
