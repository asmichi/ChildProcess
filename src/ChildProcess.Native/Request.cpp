// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Request.hpp"
#include "BinaryReader.hpp"
#include "ErrnoExceptions.hpp"
#include <cassert>
#include <cstdint>
#include <cstring>
#include <memory>
#include <vector>

namespace
{
    void GetStringArrayAndAdvance(BinaryReader& br, std::vector<const char*>* buf)
    {
        const auto count = br.Read<std::uint32_t>();
        if (count > MaxStringArrayCount)
        {
            TRACE_ERROR("count > MaxStringArrayCount: %u\n", static_cast<unsigned int>(count));
            throw BadRequestError(E2BIG);
        }

        for (std::uint32_t i = 0; i < count; i++)
        {
            buf->push_back(br.GetStringAndAdvance());
        }
    }
} // namespace

void DeserializeRequest(Request* r, std::unique_ptr<const std::byte[]> data, std::size_t length)
{
    try
    {
        BinaryReader br{data.get(), length};
        r->Data = std::move(data);
        r->Token = br.Read<std::uint64_t>();
        r->Flags = br.Read<std::uint32_t>();
        r->WorkingDirectory = br.GetStringAndAdvance();
        r->ExecutablePath = br.GetStringAndAdvance();
        GetStringArrayAndAdvance(br, &r->Argv);
        GetStringArrayAndAdvance(br, &r->Envp);

        r->Argv.push_back(nullptr);
        r->Envp.push_back(nullptr);

        if (r->ExecutablePath == nullptr)
        {
            TRACE_ERROR("ExecutablePath was nullptr.\n");
            throw BadRequestError(EINVAL);
        }
    }
    catch ([[maybe_unused]] const BadBinaryError& exn)
    {
        TRACE_ERROR("BadBinaryError: %s\n", exn.what());
        throw BadRequestError(EINVAL);
    }
}
