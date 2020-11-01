#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SrcRoot=$(dirname $0)
ProjRoot=$1
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
    (cd ${buildDir}; cmake ${SrcRoot} -G Ninja -DCMAKE_BUILD_TYPE=${configuration} -DCMAKE_TOOLCHAIN_FILE=${toolchainFile})

    ninja -C ${buildDir}

    mkdir -p ${outDir}
    cp ${buildDir}/bin/* ${outDir}
    cp ${buildDir}/lib/* ${outDir}
}

build linux-x64 Debug
build linux-x64 Release
build linux-arm Debug ${LinuxArmToolchainFile}
build linux-arm Release ${LinuxArmToolchainFile}
build linux-arm64 Debug ${LinuxArm64ToolchainFile}
build linux-arm64 Release ${LinuxArm64ToolchainFile}
