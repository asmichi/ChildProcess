// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "Base.hpp"
#include <cassert>
#include <cerrno>
#include <cstddef>
#include <sys/types.h>

// NOTE: Returns false and errno = 0 on a normal shutdown.
template<typename Func>
[[nodiscard]] bool ReadExactBytes(Func f, void* buf, std::size_t len) noexcept
{
    std::byte* byteBuf = static_cast<std::byte*>(buf);

    std::size_t offset = 0;
    while (offset < len)
    {
        ssize_t bytesRead = f(byteBuf + offset, len - offset);
        if (bytesRead == 0)
        {
            errno = 0;
            return false;
        }
        else if (bytesRead <= -1)
        {
            return false;
        }

        offset += bytesRead;
    }

    return true;
}

template<typename Func>
[[nodiscard]] bool WriteExactBytes(Func f, const void* buf, std::size_t len) noexcept
{
    const std::byte* byteBuf = static_cast<const std::byte*>(buf);

    std::size_t offset = 0;
    while (offset < len)
    {
        ssize_t bytesWritten = f(byteBuf + offset, len - offset);
        if (bytesWritten == 0)
        {
            // POSIX-conformant write & send will not return 0.
            FatalErrorAbort(errno, "write/send returned 0!");
        }
        else if (bytesWritten <= -1)
        {
            return false;
        }

        offset += bytesWritten;
    }

    return true;
}
