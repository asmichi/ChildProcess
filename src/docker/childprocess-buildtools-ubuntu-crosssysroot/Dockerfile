# Build:
#   docker build -t asmichi/childprocess-buildtools-ubuntu-crosssysroot-crosssysroot:18.04.${version} .
FROM alpine:3.13 as alpine-sysroot
ADD build-sysroot-alpine.sh /tmp/
RUN /tmp/build-sysroot-alpine.sh

FROM asmichi/childprocess-buildtools-ubuntu:18.04.20210612.1
COPY --from=alpine-sysroot /sysroots/ /sysroots/
ENV SYSROOTS_DIR="/sysroots"
