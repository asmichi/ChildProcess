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

// NOTE: Make sure to sync with the client.
enum class RequestCommand : std::uint32_t
{
    SpawnProcess = 0,
    SendSignal = 1,
};

enum class AbstractSignal : std::uint32_t
{
    Interrupt = 2,
    Kill = 9,
    Termination = 15,
};

enum SpawnProcessRequestFlags
{
    RequestFlagsRedirectStdin = 1 << 0,
    RequestFlagsRedirectStdout = 1 << 1,
    RequestFlagsRedirectStderr = 1 << 2,
    RequestFlagsCreateNewProcessGroup = 1 << 3,
    RequestFlagsEnableAutoTermination = 1 << 4,
};

struct SpawnProcessRequest final
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

struct SendSignalRequest final
{
    std::uint64_t Token;
    AbstractSignal Signal;
};

// NOTE: DeserializeSpawnProcessRequest does not set fds.
void DeserializeSpawnProcessRequest(SpawnProcessRequest* r, std::unique_ptr<const std::byte[]> data, std::size_t length);
void DeserializeSendSignalRequest(SendSignalRequest* r, std::unique_ptr<const std::byte[]> data, std::size_t length);
