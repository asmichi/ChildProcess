# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu:18.04.${version} .
FROM ubuntu:18.04
RUN sed -i -r 's!(deb|deb-src) \S+!\1 mirror+http://mirrors.ubuntu.com/mirrors.txt!' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && apt-get install -y --no-install-recommends \
        wget \
        clang-10 \
        lld-10 \
        g++ \
        g++-arm-linux-gnueabihf \
        g++-aarch64-linux-gnu \
        make \
    && rm -rf /var/lib/apt/lists/* \
    && wget https://github.com/Kitware/CMake/releases/download/v3.20.3/cmake-3.20.3-linux-x86_64.tar.gz \
    && tar -xf cmake-3.20.3-linux-x86_64.tar.gz --strip 1 -C /usr/local \
    && rm cmake-3.20.3-linux-x86_64.tar.gz
