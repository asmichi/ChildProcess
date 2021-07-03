// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "TestSignalHandler.hpp"
#include <cstdio>
#include <cstring>
#include <unistd.h>

extern char** environ;

int TestCommandDumpEnvironmentvariables(int, const char* const*)
{
    for (char** p = environ; *p != nullptr; p++)
    {
        std::size_t len = std::strlen(*p);
        if (std::fwrite(*p, sizeof(char), len + 1, stdout) != len + 1)
        {
            perror("fwrite");
            return 1;
        }
    }

    std::fflush(stdout);

    return 0;
}
