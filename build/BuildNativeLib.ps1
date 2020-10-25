# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project worktreeRoot for details.

#Requires -Version 7.0

# TODO: Embed the commit hash.

param(
    [parameter()]
    [switch]
    $AlwaysBuildImage,
    [parameter()]
    [string]
    $NamePrefix = "asmichi"
)

Set-StrictMode -Version latest

function Test-Image {
    param(
        [parameter()]
        [string]
        $ImageName
    )
    try {
        docker image inspect $ImageName 2>&1 | Out-Null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $False
    }
}

$ErrorActionPreference = "Stop"

$worktreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$linuxImageName = "${NamePrefix}/ubuntu-x64/gxx:18.04"
$buildVolumeName = "${NamePrefix}-buildnativelib-linux-x64"
$linuxContainerName = "${NamePrefix}-buildnativelib-linux-x64"

if ($AlwaysBuildImage -or -not (Test-Image $linuxImageName)) {
    Get-Content -LiteralPath "${worktreeRoot}\build\docker\ubuntu-x64-gcc-18.04.Dockerfile" | docker build -t $linuxImageName -
}

# Prepare the output directory.
$binDir = "${worktreeRoot}\bin\ChildProcess.Native\linux-x64"
New-Item -ItemType Directory -Force $binDir | Out-Null

# Build Linux binaries.
docker volume create $buildVolumeName | Out-Null
try { docker rm $linuxContainerName 2>&1 | Out-Null } catch { }
docker run `
    --mount "type=bind,readonly,source=${worktreeRoot}/src/ChildProcess.Native,target=/proj/src" `
    --mount "type=volume,src=${buildVolumeName},dst=/proj/build" `
    --mount "type=bind,source=${binDir},target=/proj/bin/linux-x64" `
    --name $linuxContainerName $linuxImageName `
    /bin/bash /proj/src/Subbuild-linux.sh /proj

docker rm $linuxContainerName | Out-Null
