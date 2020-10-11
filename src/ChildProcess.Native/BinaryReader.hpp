// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "Base.hpp"
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <exception>
#include <stdexcept>

class BadBinaryError : public MyException
{
public:
    BadBinaryError(const char* description) : MyException(description) {}
};

// Throws std::out_of_range when it would read beyond the end.
class BinaryReader final
{
public:
    BinaryReader(const void* data, std::size_t len) noexcept
        : cur_(static_cast<const std::byte*>(data)), end_(static_cast<const std::byte*>(data) + len) {}

    template<typename T>
    T Read()
    {
        T value;
        std::memcpy(&value, GetCurrentAndAdvance(sizeof(T)), sizeof(T));
        return value;
    }

    // NOTE: Pointers returned by GetString will become invalid When data becomes invalid.
    const char* GetStringAndAdvance()
    {
        const std::uint32_t bytes = Read<std::uint32_t>();
        if (bytes == 0)
        {
            return nullptr;
        }

        auto p = reinterpret_cast<const char*>(GetCurrentAndAdvance(bytes));
        if (p[bytes - 1] != '\0')
        {
            throw BadBinaryError("String not null-terminated.");
        }
        return p;
    }

private:
    const std::byte* GetCurrentAndAdvance(std::size_t bytesRead)
    {
        const auto curPos = cur_;
        const auto newPos = cur_ + bytesRead;
        if (newPos > end_)
        {
            throw BadBinaryError("Attempted to read beyond the end.");
        }

        cur_ = newPos;
        return curPos;
    }

    const std::byte* cur_;
    const std::byte* const end_;
};
