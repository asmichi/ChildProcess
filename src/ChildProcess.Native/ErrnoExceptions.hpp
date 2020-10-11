// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

class ErrnoError
{
public:
    ErrnoError(int err) noexcept : err_(err) {}
    int GetError() const noexcept { return err_; }

private:
    const int err_;
};

class CommunicationError : public ErrnoError
{
public:
    CommunicationError(int err) noexcept : ErrnoError(err) {}
};

class BadRequestError : public ErrnoError
{
public:
    BadRequestError(int err) noexcept : ErrnoError(err) {}
};
