// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "SubchannelCollection.hpp"
#include <cassert>
#include <cstdint>
#include <memory>
#include <unordered_map>

SubchannelCollection::~SubchannelCollection()
{
    const std::lock_guard<std::mutex> guard(mapMutex_);

    if (map_.size() > 0)
    {
        // ~SubchannelCollection and Delete are racing.
        // We need to wait for all subchannels to finish before exiting.
        TRACE_ERROR("Exiting while subchannel(s) are still running. (%zu remaining)\n", map_.size());

        // Try to at least prevent double-free (although we are already in a broken state).
        map_.clear();
    }
}

Subchannel* SubchannelCollection::Add(std::unique_ptr<Subchannel> subchannel)
{
    const std::lock_guard<std::mutex> guard(mapMutex_);

    const auto [it, inserted] = map_.insert(std::pair{subchannel.get(), std::move(subchannel)});
    if (!inserted)
    {
        FatalErrorAbort("Attempted to register a Subchannel twice.");
    }

    return (*it).first;
}

void SubchannelCollection::Delete(Subchannel* key)
{
    const std::lock_guard<std::mutex> guard(mapMutex_);

    const auto it = map_.find(key);
    assert(it != map_.end());
    map_.erase(it);
}
