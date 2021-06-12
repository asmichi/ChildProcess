# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu:18.04.${version} .
#
# NOTE: clang-10 installs libstdc++-7-dev, etc. as its dependencies. 
FROM ubuntu:18.04
RUN sed -i -r 's!(deb|deb-src) \S+!\1 mirror+http://mirrors.ubuntu.com/mirrors.txt!' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && apt-get install -y --no-install-recommends \
        wget \
        clang-10 \
        lld-10 \
        make \
        libc6-arm64-cross \
        libc6-dev-arm64-cross \
        libgcc1-arm64-cross \
        libstdc++-7-dev-arm64-cross \
        libstdc++6-arm64-cross \
        linux-libc-dev-arm64-cross \
        libc6-armhf-cross \
        libc6-dev-armhf-cross \
        libgcc1-armhf-cross \
        libstdc++-7-dev-armhf-cross \
        libstdc++6-armhf-cross \
        linux-libc-dev-armhf-cross \
    && rm -rf /var/lib/apt/lists/* \
    && wget https://github.com/Kitware/CMake/releases/download/v3.20.3/cmake-3.20.3-linux-x86_64.tar.gz \
    && tar -xf cmake-3.20.3-linux-x86_64.tar.gz --strip 1 -C /usr/local \
    && rm cmake-3.20.3-linux-x86_64.tar.gz

