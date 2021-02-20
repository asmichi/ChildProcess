// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "Subchannel.hpp"
#include <memory>
#include <mutex>
#include <unordered_map>

class SubchannelCollection final
{
public:
    ~SubchannelCollection();
    Subchannel* Add(std::unique_ptr<Subchannel> subchannel);
    void Delete(Subchannel* key);
    size_t Size() const { return map_.size(); }

private:
    // Serializes lookup, insertion and removal.
    mutable std::mutex mapMutex_;
    std::unordered_map<Subchannel*, std::unique_ptr<Subchannel>> map_;
};
