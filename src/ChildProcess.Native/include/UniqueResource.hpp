// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include <unistd.h>

// Represents unique ownership of a kind of resource defined by UniqueResourcePolicy.
//
// Unlike unique_ptr, because it is not a pointer, it does not provide operator->.
template<typename UniqueResourcePolicy>
class UniqueResourceImpl final
{
public:
    using ThisType = UniqueResourceImpl<UniqueResourcePolicy>;
    using ValueType = typename UniqueResourcePolicy::ValueType;

    explicit UniqueResourceImpl(ValueType value) noexcept : _value(value) {}
    UniqueResourceImpl(ThisType&& other) noexcept : UniqueResourceImpl(other.Release()) {}
    UniqueResourceImpl() noexcept : _value(UniqueResourcePolicy::NullValue) {}
    UniqueResourceImpl(const ThisType&) = delete;
    ~UniqueResourceImpl() noexcept { Reset(); }

    ThisType& operator=(const ThisType&) = delete;
    ThisType& operator=(ThisType&& other) noexcept
    {
        Reset(other.Release());
        return *this;
    }

    ValueType Get() const noexcept { return _value; }
    bool IsValid() const noexcept { return UniqueResourcePolicy::IsValid(_value); }

    void Reset(ValueType newValue = UniqueResourcePolicy::NullValue) noexcept
    {
        if (IsValid())
        {
            UniqueResourcePolicy::Delete(_value);
        }

        _value = newValue;
    }

    [[nodiscard]] ValueType Release() noexcept
    {
        auto tmp = _value;
        _value = UniqueResourcePolicy::NullValue;
        return tmp;
    }

private:
    ValueType _value;
};

struct FileDescriptorUniqueResourcePolicy final
{
    using ValueType = int;

    static constexpr ValueType NullValue = -1;

    static bool IsValid(const ValueType& value) noexcept
    {
        return value >= 0;
    }

    static void Delete(const ValueType& value) noexcept
    {
        if (IsValid(value))
        {
            ::close(value);
        }
    }
};

// UniqueResource with the file descriptor semantics.
using UniqueFd = UniqueResourceImpl<FileDescriptorUniqueResourcePolicy>;
