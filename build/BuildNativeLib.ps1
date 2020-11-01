# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project worktreeRoot for details.

#Requires -Version 7.0

# TODO: Embed the commit hash.

param(
    [switch]
    [bool]
    $Rebuild = $false,
    [parameter()]
    [string]
    $NamePrefix = "asmichi"
)

Set-StrictMode -Version latest

$ErrorActionPreference = "Stop"

$worktreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$linuxImageName = "asmichi/childprocess-buildtools-ubuntu:18.04.20201102.1"
$buildVolumeName = "${NamePrefix}-buildnativelib-linux"

# Prepare the output directory.
$binDir = "${worktreeRoot}\bin\ChildProcess.Native"
New-Item -ItemType Directory -Force $binDir | Out-Null

# Build Linux binaries.
if ($Rebuild) {
    try {
        docker volume rm $buildVolumeName 2>&1 | Out-Null
    }
    catch {}
}

docker volume create $buildVolumeName | Out-Null
docker run `
    --mount "type=bind,readonly,source=${worktreeRoot}/src/ChildProcess.Native,target=/proj/src" `
    --mount "type=volume,src=${buildVolumeName},dst=/proj/build" `
    --mount "type=bind,source=${binDir},target=/proj/bin" `
    --rm $linuxImageName `
    /bin/bash /proj/src/Subbuild-linux.sh /proj
