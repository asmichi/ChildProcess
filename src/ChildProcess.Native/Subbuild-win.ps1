# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [string]
    $WorktreeRoot
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$SrcRoot = $PSScriptRoot

function Start-Build() {
    param(
        [parameter()]
        [string]
        $Arch,
        [parameter()]
        [string]
        $Configuration
    )

    Start-ThreadJob -ScriptBlock {
        $Arch = $using:Arch
        $Configuration = $using:Configuration
        $WorktreeRoot = $using:WorktreeRoot
        $SrcRoot = $using:SrcRoot

        $rid = "win-${Arch}"
        $buildDir = Join-Path $WorktreeRoot "obj/ChildProcess.Native/$rid/${Configuration}"
        $outDir = Join-Path $WorktreeRoot "bin/ChildProcess.Native/$rid/${Configuration}"

        New-Item -ItemType Directory -Force $buildDir | Out-Null
        Push-Location -LiteralPath $buildDir
        # --no-warn-unused-cli: https://gitlab.kitware.com/cmake/cmake/-/issues/17261
        cmake $SrcRoot -G Ninja --no-warn-unused-cli "-DCMAKE_BUILD_TYPE=${Configuration}" "-DCMAKE_TOOLCHAIN_FILE=${SrcRoot}/cmake/toolchain-win-${Arch}.cmake"
        Pop-Location

        ninja -C $buildDir

        if ($LASTEXITCODE -ne 0) {
            Write-Error "build failed ($Arch $Configuration)"
        }
        else {
            New-Item -ItemType Directory -Force $outDir | Out-Null
            Copy-Item "$buildDir/bin/*" -Destination $outDir
        }
    }
}

@(
    Start-Build -Arch x86 -Configuration Debug
    Start-Build -Arch x86 -Configuration Release
    Start-Build -Arch x64 -Configuration Debug
    Start-Build -Arch x64 -Configuration Release
) | Receive-Job -Wait -AutoRemoveJob
