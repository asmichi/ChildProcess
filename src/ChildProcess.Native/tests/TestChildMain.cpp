// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include <cstdio>
#include <cstring>

// *nix-specific startup code
#if !defined(_WIN32)
extern void SetupDefaultTestSignalHandlers();
#endif

// Handlers
extern int TestCommandReportSignal(int argc, const char* const* argv);
#if defined(_WIN32)
#else
#endif

namespace
{
    struct TestCommandDefinition
    {
        const char* const SubcommandName;
        int (*const Handler)(int argc, const char* const* argv);
    };

    TestCommandDefinition TestCommandDefinitions[] = {
        {"ReportSignal", TestCommandReportSignal},
#if defined(_WIN32)
#else
#endif
    };
} // namespace

int main(int argc, const char** argv)
{
    if (argc < 2)
    {
        std::fprintf(stderr, "Usage: TestChildNative name [args]\n");
        return 1;
    }

#if !defined(_WIN32)
    SetupDefaultTestSignalHandlers();
#endif

    const char* const subcommand = argv[1];
    for (const auto& def : TestCommandDefinitions)
    {
        if (strcmp(subcommand, def.SubcommandName) == 0)
        {
            return def.Handler(argc, argv);
        }
    }

    std::fprintf(stderr, "error: Unknown subcommand '%s'\n", subcommand);
    return 1;
}
