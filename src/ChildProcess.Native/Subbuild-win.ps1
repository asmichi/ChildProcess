# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [string]
    $WorktreeRoot
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$srcRoot = $PSScriptRoot

function build() {
    param(
        [parameter()]
        [string]
        $Arch,
        [parameter()]
        [string]
        $Configuration
    )

    $rid = "win-${Arch}"
    $buildDir = Join-Path $WorktreeRoot "obj/ChildProcess.Native/$rid/${Configuration}"
    $outDir = Join-Path $WorktreeRoot "bin/ChildProcess.Native/$rid/${Configuration}"

    New-Item -ItemType Directory -Force $buildDir | Out-Null
    Push-Location -LiteralPath $buildDir
    # --no-warn-unused-cli: https://gitlab.kitware.com/cmake/cmake/-/issues/17261
    cmake $srcRoot -G Ninja --no-warn-unused-cli "-DCMAKE_BUILD_TYPE=${Configuration}" "-DCMAKE_TOOLCHAIN_FILE=${srcRoot}/cmake/toolchain-msvc-${Arch}.cmake"
    Pop-Location

    ninja -C $buildDir

    New-Item -ItemType Directory -Force $outDir | Out-Null
    Copy-Item "$buildDir/bin/*" -Destination $outDir
}

build -Arch x86 -Configuration Debug
build -Arch x86 -Configuration Release
build -Arch x64 -Configuration Debug
build -Arch x64 -Configuration Release
