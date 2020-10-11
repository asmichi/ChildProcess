FROM ubuntu:18.04
RUN apt-get update && apt-get install -y \
    cmake \
    g++ \
    gcc \
    ninja-build \
    && rm -rf /var/lib/apt/lists/*