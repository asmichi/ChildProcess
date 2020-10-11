// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "WriteBuffer.hpp"
#include <cassert>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <memory>
#include <optional>
#include <queue>
#include <tuple>
#include <vector>

namespace
{
    const constexpr std::size_t BlockLength = 32 * 1024;
} // namespace

void WriteBuffer::Enqueue(const void* buf, std::size_t len)
{
    auto* byteBuf = static_cast<const std::byte*>(buf);
    auto* const pEnd = byteBuf + len;
    if (!blocks_.empty())
    {
        auto& back = blocks_.back();
        byteBuf += StoreToBlock(&back, byteBuf, len);
        assert(byteBuf <= pEnd);
    }

    while (byteBuf < pEnd)
    {
        auto b = CreateBlock();
        byteBuf += StoreToBlock(&b, byteBuf, pEnd - byteBuf);
        blocks_.push_back(std::move(b));
    }

    assert(byteBuf == pEnd);
}

void WriteBuffer::Dequeue(std::size_t len) noexcept
{
    while (len != 0)
    {
        auto& front = blocks_.front();
        const auto remainingBytes = front.DataBytes - front.CurrentOffset;
        if (remainingBytes <= len)
        {
            blocks_.erase(blocks_.begin());
            len -= remainingBytes;
        }
        else
        {
            front.CurrentOffset += len;
            len = 0;
        }
    }

    assert(false);
}

std::tuple<std::byte*, std::size_t> WriteBuffer::GetPendingData() noexcept
{
    if (blocks_.empty())
    {
        std::abort();
    }

    const auto& first = blocks_.front();
    return std::make_tuple(first.Data.get() + first.CurrentOffset, first.DataBytes - first.CurrentOffset);
}

WriteBuffer::Block WriteBuffer::CreateBlock()
{
    Block b;
    b.Data = std::make_unique<std::byte[]>(BlockLength);
    b.DataBytes = 0;
    b.CurrentOffset = 0;
    return std::move(b);
}

std::size_t WriteBuffer::StoreToBlock(Block* pBlock, const std::byte* buf, std::size_t len) noexcept
{
    const auto freeBytes = BlockLength - pBlock->DataBytes;
    const auto bytesToStore = std::min(freeBytes, len);

    std::memcpy(pBlock->Data.get() + pBlock->DataBytes, buf, bytesToStore);
    pBlock->DataBytes += bytesToStore;
    assert(pBlock->DataBytes <= BlockLength);

    return bytesToStore;
}
