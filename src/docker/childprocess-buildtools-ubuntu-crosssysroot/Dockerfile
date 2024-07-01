# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu-crosssysroot:22.04.${version} .
FROM alpine:3.13 as alpine-sysroot
ADD build-sysroot-alpine.sh /tmp/
RUN /tmp/build-sysroot-alpine.sh

FROM ubuntu:18.04 as ubuntu18-sysroot
RUN sed -i -r 's!(deb|deb-src) \S+!\1 mirror+http://mirrors.ubuntu.com/mirrors.txt!' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && apt-get install -y --no-install-recommends \
        libc6-amd64-cross \
        libc6-dev-amd64-cross \
        libgcc1-amd64-cross \
        libstdc++-7-dev-amd64-cross \
        libstdc++6-amd64-cross \
        linux-libc-dev-amd64-cross \
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
    && rm -rf /var/lib/apt/lists/*
ADD extract-sysroot-ubuntu18.sh /tmp/
RUN /tmp/extract-sysroot-ubuntu18.sh

FROM asmichi/childprocess-buildtools-ubuntu:22.04.20240702.1
COPY --from=alpine-sysroot /sysroots/ /sysroots/
COPY --from=ubuntu18-sysroot /sysroots/ /sysroots/
ENV SYSROOTS_DIR="/sysroots"
