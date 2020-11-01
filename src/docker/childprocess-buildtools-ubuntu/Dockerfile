# Build:
#   launch your apt_cacher_ng (rewrite the address in the following command)
#   docker build -t asmichi/buildtools-ubuntu-gxx:18.04 --build-arg apt_cacher_ng_host=192.168.1.65:3142 .
FROM ubuntu:18.04
ARG apt_cacher_ng_host=192.168.1.65:3142
RUN echo "Acquire::http { Proxy \"http://${apt_cacher_ng_host}\"; };" > /etc/apt/apt.conf.d/01proxy \
    && apt-get update && apt-get install -y --no-install-recommends \
        cmake \
        g++ \
        g++-arm-linux-gnueabihf \
        g++-aarch64-linux-gnu \
        ninja-build \
    && rm -rf /var/lib/apt/lists/* \
    && rm /etc/apt/apt.conf.d/01proxy
