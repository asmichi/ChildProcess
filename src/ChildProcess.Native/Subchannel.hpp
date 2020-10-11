// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "UniqueResource.hpp"
#include <cstdint>

const std::uint32_t MaxReqeuestLength = 2 * 1024 * 1024;

void StartSubchannelHandler(UniqueFd sockFd);
