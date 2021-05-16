#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SrcRoot=$(dirname $0)
ProjRoot=$1
LinuxX64ToolchainFile=${SrcRoot}/cmake/toolchain-x64-linux.cmake
LinuxArmToolchainFile=${SrcRoot}/cmake/toolchain-arm-linux-gnueabihf.cmake
LinuxArm64ToolchainFile=${SrcRoot}/cmake/toolchain-aarch64-linux-gnu.cmake

function build()
{
    local rid=$1
    local configuration=$2
    local toolchainFile=$3
    local buildDir=${ProjRoot}/build/${rid}/${configuration}
    local outDir=${ProjRoot}/bin/${rid}/${configuration}

    mkdir -p ${buildDir}
    (cd ${buildDir}; cmake ${SrcRoot} -G Ninja -DCMAKE_BUILD_TYPE:STRING=${configuration} -DCMAKE_TOOLCHAIN_FILE:FILEPATH=${toolchainFile} -DCMAKE_CXX_COMPILER:FILEPATH=/usr/bin/clang++-10)

    ninja -C ${buildDir}

    mkdir -p ${outDir}
    cp ${buildDir}/bin/* ${outDir}
    cp ${buildDir}/lib/* ${outDir}
}

build linux-x64 Debug ${LinuxX64ToolchainFile} &
build linux-x64 Release ${LinuxX64ToolchainFile} &
build linux-arm Debug ${LinuxArmToolchainFile} &
build linux-arm Release ${LinuxArmToolchainFile} &
build linux-arm64 Debug ${LinuxArm64ToolchainFile} &
build linux-arm64 Release ${LinuxArm64ToolchainFile} &

wait
