// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <cerrno>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <fcntl.h>
#include <io.h>
#include <memory>

int TestCommandDumpEnvironmentVariables(int argc, const char* const* argv)
{
    if (_setmode(_fileno(stdout), O_BINARY) == -1)
    {
        perror("_setmode");
        return 1;
    }

    wchar_t* pFirstEnv = GetEnvironmentStringsW();
    if (pFirstEnv == nullptr)
    {
        return 1;
    }

    wchar_t* pEnv = pFirstEnv;
    while (*pEnv != '\0')
    {
        const std::size_t len = wcslen(pEnv);
        if (len > UNICODE_STRING_MAX_CHARS)
        {
            std::fprintf(stderr, "Broken environment block.\n");
            return 1;
        }

        if (*pEnv == '=')
        {
            // ignore hidden environment variables
            pEnv += len + 1;
            continue;
        }

        const int requiredBytes = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, pEnv, static_cast<int>(len) + 1, nullptr, 0, nullptr, nullptr);
        auto buf = std::make_unique<char[]>(requiredBytes);

        const int actualBytes = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, pEnv, static_cast<int>(len) + 1, buf.get(), requiredBytes, nullptr, nullptr);
        if (actualBytes == 0)
        {
            std::fprintf(stderr, "WideCharToMultiByte failed with %d.\n", GetLastError());
            return 1;
        }

        if (std::fwrite(buf.get(), sizeof(char), actualBytes, stdout) != actualBytes)
        {
            perror("fwrite");
            return 1;
        }

        pEnv += len + 1;
    }

    std::fflush(stdout);
    FreeEnvironmentStringsW(pFirstEnv);
    return 0;
}
