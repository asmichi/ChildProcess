# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu:22.04.${version} .
FROM ubuntu:22.04
RUN sed -i -r 's!(deb|deb-src) \S+!\1 mirror+http://mirrors.ubuntu.com/mirrors.txt!' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && apt-get install -y --no-install-recommends \
        clang \
        lld \
        make \
        cmake \
    && rm -rf /var/lib/apt/lists/*
