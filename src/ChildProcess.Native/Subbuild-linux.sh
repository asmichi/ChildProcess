#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SrcRoot=$(dirname $0)
ProjRoot=$1

function build()
{
    local rid=$1
    local configuration=$2
    local buildDir=${ProjRoot}/build/${rid}/${configuration}
    local outDir=${ProjRoot}/bin/${rid}/${configuration}

    mkdir -p $buildDir
    (cd $buildDir; cmake $SrcRoot -G Ninja -DCMAKE_BUILD_TYPE=${configuration})

    ninja -C $buildDir

    mkdir -p $outDir
    cp $buildDir/bin/* $outDir
    cp $buildDir/lib/* $outDir
}

build linux-x64 Debug
build linux-x64 Release
