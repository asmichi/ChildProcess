# Assume x64 Ubuntu 22.04 & Clang & LLD & Ubuntu 18 sysroot
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR arm)

set(CMAKE_CXX_COMPILER /usr/bin/clang++)
set(CMAKE_CXX_COMPILER_TARGET arm-linux-gnueabihf)
set(CMAKE_SYSROOT $ENV{SYSROOTS_DIR}/sysroot-ubuntu18)
include_directories(SYSTEM
    ${CMAKE_SYSROOT}/usr/arm-linux-gnueabihf/include/c++/7.5.0
    ${CMAKE_SYSROOT}/usr/arm-linux-gnueabihf/include/c++/7.5.0/arm-linux-gnueabihf
    ${CMAKE_SYSROOT}/usr/arm-linux-gnueabihf/include/c++/7.5.0/backward
    ${CMAKE_SYSROOT}/usr/arm-linux-gnueabihf/include
    /usr/lib/llvm-14/lib/clang/14.0.0/include)

# Workaround for https://gitlab.kitware.com/cmake/cmake/-/issues/17966
unset(CMAKE_CXX_IMPLICIT_INCLUDE_DIRECTORIES)

add_compile_options(-nostdinc)
add_link_options(-fuse-ld=lld)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
