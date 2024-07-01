#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

set -eu

root=/sysroots/sysroot-ubuntu18
triples="x86_64-linux-gnu arm-linux-gnueabihf aarch64-linux-gnu"

function copy_dirs()
{
    local src_parent=/${1}
    local dst_parent=${root}/${1}
    local dirs=${@:2}

    mkdir -p ${dst_parent}

    for x in $dirs; do
        cp -R ${src_parent}/${x} ${dst_parent}
    done
}

function copy_files()
{
    local src_parent=/${1}
    local dst_parent=${root}/${1}
    local files=${@:2}

    mkdir -p ${dst_parent}

    for x in $files; do
        cp -P ${src_parent}/${x} ${dst_parent}/${x}
    done
}

if [ -e ${root} ]; then
    rm -rf ${root}
fi

# headers
for triple in ${triples}; do
    copy_dirs usr/${triple} include
done

# libs
for triple in ${triples}; do
    copy_dirs usr/lib/gcc-cross/${triple} 7
    # Remove unnecessary huge static libraries to save space
    rm ${root}/usr/lib/gcc-cross/${triple}/7/*.a
    copy_files usr/lib/gcc-cross/${triple}/7 libgcc.a

    copy_dirs usr/${triple} lib
    # Remove unnecessary huge static libraries to save space
    rm ${root}/usr/${triple}/lib/*.a
    copy_files usr/${triple}/lib libc_nonshared.a libpthread_nonshared.a
done

copy_files usr/x86_64-linux-gnu/lib libmvec_nonshared.a
