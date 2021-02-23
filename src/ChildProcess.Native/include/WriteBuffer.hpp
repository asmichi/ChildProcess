// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include <cstddef>
#include <memory>
#include <optional>
#include <tuple>
#include <vector>

class WriteBuffer final
{
public:
    void Enqueue(const void* buf, std::size_t len);
    void Dequeue(std::size_t len) noexcept;
    bool HasPendingData() noexcept { return !blocks_.empty(); }
    std::tuple<std::byte*, std::size_t> GetPendingData() noexcept;

private:
    struct Block
    {
        std::unique_ptr<std::byte[]> Data;
        std::size_t DataBytes;
        std::size_t CurrentOffset;
    };

    Block CreateBlock();
    std::size_t StoreToBlock(Block* pBlock, const std::byte* pSrc, std::size_t len) noexcept;

    std::vector<Block> blocks_;
};
