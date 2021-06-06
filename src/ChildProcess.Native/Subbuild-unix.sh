#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SrcRoot=$(dirname $0)
ProjRoot=$1
LinuxX64ToolchainFile=${SrcRoot}/cmake/toolchain-x64-linux.cmake
LinuxArmToolchainFile=${SrcRoot}/cmake/toolchain-arm-linux-gnueabihf.cmake
LinuxArm64ToolchainFile=${SrcRoot}/cmake/toolchain-aarch64-linux-gnu.cmake
Jobs=$(getconf _NPROCESSORS_ONLN)
pids=()

function build_impl()
{
    local rid=$1
    local configuration=$2
    local toolchainFile=$3
    local buildDir=${ProjRoot}/build/${rid}/${configuration}
    local outDir=${ProjRoot}/bin/${rid}/${configuration}

    mkdir -p ${buildDir}
    (cd ${buildDir}; cmake ${SrcRoot} -G "Unix Makefiles" -DCMAKE_BUILD_TYPE:STRING=${configuration} -DCMAKE_TOOLCHAIN_FILE:FILEPATH=${toolchainFile} -DCMAKE_CXX_COMPILER:FILEPATH=/usr/bin/clang++-10) || return

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

build linux-x64 Debug ${LinuxX64ToolchainFile}
build linux-x64 Release ${LinuxX64ToolchainFile}
build linux-arm Debug ${LinuxArmToolchainFile}
build linux-arm Release ${LinuxArmToolchainFile}
build linux-arm64 Debug ${LinuxArm64ToolchainFile}
build linux-arm64 Release ${LinuxArm64ToolchainFile}

status=0
for pid in ${pids[*]}; do
    wait $pid
    if [ $? -ne 0 ]; then
        status=1
    fi
done

exit $status
