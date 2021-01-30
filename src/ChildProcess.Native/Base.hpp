// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

// Common implementations.

#include <errno.h>
#include <exception>

class MyException : public std::exception
{
public:
    MyException(const char* description) : description_(description) {}
    virtual const char* what() const noexcept override { return description_; }

private:
    const char* const description_;
};

enum class BlockingFlag : bool
{
    Blocking = false,
    NonBlocking = true,
};

void PutFatalError(const char* str) noexcept;
void PutFatalError(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorAbort(const char* str) noexcept;
[[noreturn]] void FatalErrorAbort(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorExit(int err, const char* str) noexcept;

[[nodiscard]] inline bool IsWouldBlockError(int err) { return err == EAGAIN || err == EWOULDBLOCK; }
[[nodiscard]] inline bool IsConnectionClosedError(int err) noexcept { return err == ECONNRESET || err == EPIPE; }

#if defined(ENABLE_TRACE_DEBUG)
#define TRACE_DEBUG(format, ...) static_cast<void>(std::fprintf(stderr, "[ChildProcess] debug: " format, ##__VA_ARGS__))
#else
#define TRACE_DEBUG(format, ...) static_cast<void>(0)
#endif

#if defined(ENABLE_TRACE_INFO)
#define TRACE_INFO(format, ...) static_cast<void>(std::fprintf(stderr, "[ChildProcess] info: " format, ##__VA_ARGS__))
#else
#define TRACE_INFO(format, ...) static_cast<void>(0)
#endif

#if defined(ENABLE_TRACE_ERROR)
#define TRACE_ERROR(format, ...) static_cast<void>(std::fprintf(stderr, "[ChildProcess] error: " format, ##__VA_ARGS__))
#else
#define TRACE_ERROR(format, ...) static_cast<void>(0)
#endif

#define TRACE_FATAL(format, ...) static_cast<void>(std::fprintf(stderr, "[ChildProcess] fatal error:" format, ##__VA_ARGS__))
