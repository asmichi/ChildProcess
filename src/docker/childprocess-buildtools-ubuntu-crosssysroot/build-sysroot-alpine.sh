#!/bin/sh
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

set -eu

function build_sysroot()
{
    local arch=$1
    local sysroot_name=sysroot-alpine-${arch}-alpine-linux-musl
    local tmp_root=/tmp/${sysroot_name}
    local root=/sysroots/${sysroot_name}

    if [ -e ${tmp_root} ]; then
        rm -rf ${tmp_root}
    fi
    if [ -e ${root} ]; then
        rm -rf ${root}
    fi

    apk --root ${tmp_root} --keys-dir /usr/share/apk/keys --repositories-file /etc/apk/repositories --arch ${arch} --initdb add \
        linux-headers libgcc libc-dev libstdc++ gcc g++

    rm -rf ${tmp_root}/lib/apk
    # Remove unnecessary huge static libraries to save space
    rm ${tmp_root}/usr/lib/libstdc++.a ${tmp_root}/usr/lib/libc.a

    mkdir -p ${root}
    mkdir -p ${root}/usr
    mkdir -p ${root}/usr

    cp -R ${tmp_root}/lib ${root}/
    cp -R ${tmp_root}/usr/include ${root}/usr
    cp -R ${tmp_root}/usr/lib ${root}/usr

    rm -rf ${tmp_root}
}

build_sysroot x86_64
build_sysroot aarch64
