# Assume x64 Ubuntu 18.04 & Clang 10 & LLD
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR arm)

set(CMAKE_CXX_COMPILER /usr/bin/clang++-10)
set(CMAKE_CXX_COMPILER_TARGET arm-linux-gnueabihf)
set(CMAKE_SYSROOT /usr/arm-linux-gnueabihf)
include_directories(SYSTEM
    /usr/arm-linux-gnueabihf/include/c++/7
    /usr/arm-linux-gnueabihf/include/c++/7/arm-linux-gnueabihf
    /usr/arm-linux-gnueabihf/include/c++/7/backward
    /usr/arm-linux-gnueabihf/include
    /usr/lib/llvm-10/lib/clang/10.0.0/include)

add_compile_options(-nostdinc)
add_link_options(-fuse-ld=lld)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
