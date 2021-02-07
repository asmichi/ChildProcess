// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

// NOTE: Make sure to sync with the client.
enum ErrorCode : int
{
    InvalidRequest = -1,
};

// 0: Success
// Positive: errno
// Negative: ErrorCode
class ErrorCodeException
{
public:
    ErrorCodeException(int err) noexcept : err_(err) {}
    ErrorCodeException(ErrorCode err) noexcept : err_(static_cast<int>(err)) {}
    int GetError() const noexcept { return err_; }

private:
    const int err_;
};

class CommunicationError : public ErrorCodeException
{
public:
    CommunicationError(int err) noexcept : ErrorCodeException(err) {}
    CommunicationError(ErrorCode err) noexcept : ErrorCodeException(err) {}
};

class BadRequestError : public ErrorCodeException
{
public:
    BadRequestError(int err) noexcept : ErrorCodeException(err) {}
    BadRequestError(ErrorCode err) noexcept : ErrorCodeException(err) {}
};
