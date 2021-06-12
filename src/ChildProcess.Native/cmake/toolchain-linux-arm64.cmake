# Assume x64 Ubuntu 18.04 & Clang 10 & LLD
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR aarch64)

set(CMAKE_CXX_COMPILER /usr/bin/clang++-10)
set(CMAKE_CXX_COMPILER_TARGET aarch64-linux-gnu)
set(CMAKE_SYSROOT /usr/aarch64-linux-gnu)
include_directories(SYSTEM
    /usr/aarch64-linux-gnu/include/c++/7
    /usr/aarch64-linux-gnu/include/c++/7/aarch64-linux-gnu
    /usr/aarch64-linux-gnu/include/c++/7/backward
    /usr/aarch64-linux-gnu/include
    /usr/lib/llvm-10/lib/clang/10.0.0/include)

add_compile_options(-nostdinc)
add_link_options(-fuse-ld=lld)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
