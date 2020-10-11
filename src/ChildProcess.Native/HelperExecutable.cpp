// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

// This file is the only file not included in the library.
// The sole purpose is to produce an executable that invokes HelperMain of the library.

extern "C" int HelperMain(int argc, const char** argv);

int main(int argc, const char** argv)
{
    return HelperMain(argc, argv);
}
