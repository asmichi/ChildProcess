#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

set -eu

SrcRoot=$(dirname $0)
OS=$1
ObjDir=$2
BinDir=$3
ToolchainFile_LinuxX64=${SrcRoot}/cmake/toolchain-linux-x64.cmake
ToolchainFile_LinuxArm=${SrcRoot}/cmake/toolchain-linux-arm.cmake
ToolchainFile_LinuxArm64=${SrcRoot}/cmake/toolchain-linux-arm64.cmake
ToolchainFile_OSXX64=${SrcRoot}/cmake/toolchain-osx-x64.cmake
ToolchainFile_OSXArm64=${SrcRoot}/cmake/toolchain-osx-arm64.cmake
Jobs=$(getconf _NPROCESSORS_ONLN)
pids=()

function build_impl()
{
    local rid=$1
    local configuration=$2
    local toolchainFile=$3
    local extraArgs=${@:4}
    local buildDir=${ObjDir}/${rid}/${configuration}
    local outDir=${BinDir}/${rid}/${configuration}

    mkdir -p ${buildDir}
    (cd ${buildDir}; cmake ${SrcRoot} -G "Unix Makefiles" -DCMAKE_BUILD_TYPE:STRING=${configuration} -DCMAKE_TOOLCHAIN_FILE:FILEPATH=${toolchainFile} $extraArgs) || return

    make -C ${buildDir} -j ${Jobs} || return

    mkdir -p ${outDir}
    cp ${buildDir}/bin/* ${outDir}
    cp ${buildDir}/lib/* ${outDir}
}

function build()
{
    build_impl "$@" &
    pids+=($!)
}

case ${OS} in
    "Linux")
        build linux-x64 Debug ${ToolchainFile_LinuxX64}
        build linux-x64 Release ${ToolchainFile_LinuxX64}
        build linux-arm Debug ${ToolchainFile_LinuxArm}
        build linux-arm Release ${ToolchainFile_LinuxArm}
        build linux-arm64 Debug ${ToolchainFile_LinuxArm64}
        build linux-arm64 Release ${ToolchainFile_LinuxArm64}
        ;;
    "OSX")
        build osx-x64 Debug ${ToolchainFile_OSXX64} -DCMAKE_OSX_ARCHITECTURES=x86_64
        build osx-x64 Release ${ToolchainFile_OSXX64} -DCMAKE_OSX_ARCHITECTURES=x86_64
        build osx-arm64 Debug ${ToolchainFile_OSXArm64} -DCMAKE_OSX_ARCHITECTURES=arm64
        build osx-arm64 Release ${ToolchainFile_OSXArm64} -DCMAKE_OSX_ARCHITECTURES=arm64
        ;;
    *)
        echo "Unknown OS: ${OS}" 1>&2
        exit 1
        ;;
esac

status=0
for pid in ${pids[*]}; do
    wait $pid
    if [ $? -ne 0 ]; then
        status=1
    fi
done

exit $status
