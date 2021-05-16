# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu:18.04.${version} 
FROM ubuntu:18.04
RUN sed -i -r 's!(deb|deb-src) \S+!\1 mirror+http://mirrors.ubuntu.com/mirrors.txt!' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && apt-get install -y --no-install-recommends \
        cmake \
        g++ \
        g++-arm-linux-gnueabihf \
        g++-aarch64-linux-gnu \
        ninja-build \
    && rm -rf /var/lib/apt/lists/*
