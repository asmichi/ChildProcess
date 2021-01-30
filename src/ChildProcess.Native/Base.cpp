// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Base.hpp"
#include <cerrno>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <unistd.h>

void PutFatalError(const char* str) noexcept
{
    std::fprintf(stderr, "[ChildProcess] fatal error: %s\n", str);
}

void PutFatalError(int err, const char* str) noexcept
{
    errno = err;
    std::fputs("[ChildProcess] fatal error: ", stderr);
    perror(str);
}

void FatalErrorAbort(const char* str) noexcept
{
    PutFatalError(str);
    std::abort();
}

void FatalErrorAbort(int err, const char* str) noexcept
{
    PutFatalError(err, str);
    std::abort();
}

void FatalErrorExit(int err, const char* str) noexcept
{
    PutFatalError(err, str);
    std::exit(1);
}
