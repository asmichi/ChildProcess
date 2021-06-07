# Assume x64 Ubuntu 18.04 & Clang 10 & LLD
set(CMAKE_CXX_COMPILER /usr/bin/clang++-10)
add_link_options(-fuse-ld=lld)
