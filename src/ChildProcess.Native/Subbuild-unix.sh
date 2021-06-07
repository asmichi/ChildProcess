#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SrcRoot=$(dirname $0)
ProjRoot=$1
OS=$2
LinuxX64ToolchainFile=${SrcRoot}/cmake/toolchain-x64-linux.cmake
LinuxArmToolchainFile=${SrcRoot}/cmake/toolchain-arm-linux-gnueabihf.cmake
LinuxArm64ToolchainFile=${SrcRoot}/cmake/toolchain-aarch64-linux-gnu.cmake
OSXX64ToolchainFile=${SrcRoot}/cmake/toolchain-x64-osx.cmake
OSXArm64ToolchainFile=${SrcRoot}/cmake/toolchain-arm64-osx.cmake
Jobs=$(getconf _NPROCESSORS_ONLN)
pids=()

function build_impl()
{
    local rid=$1
    local configuration=$2
    local toolchainFile=$3
    local extraArgs=${@:4}
    local buildDir=${ProjRoot}/obj/${rid}/${configuration}
    local outDir=${ProjRoot}/bin/${rid}/${configuration}

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
        build linux-x64 Debug ${LinuxX64ToolchainFile}
        build linux-x64 Release ${LinuxX64ToolchainFile}
        build linux-arm Debug ${LinuxArmToolchainFile}
        build linux-arm Release ${LinuxArmToolchainFile}
        build linux-arm64 Debug ${LinuxArm64ToolchainFile}
        build linux-arm64 Release ${LinuxArm64ToolchainFile}
        ;;
    "OSX")
        CMAKE_FLAGS=""
        build osx-x64 Debug ${OSXX64ToolchainFile}
        build osx-x64 Release ${OSXX64ToolchainFile}
        build osx-arm64 Debug ${OSXArm64ToolchainFile}
        build osx-arm64 Release ${OSXArm64ToolchainFile}
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
