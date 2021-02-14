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
$linuxContainerName = "${NamePrefix}-buildnativelib-linux"
$buildVolumeName = "${NamePrefix}-buildnativelib-linux"

# Prepare the output directory.
$objDir = "${worktreeRoot}\obj\ChildProcess.Native"
$binDir = "${worktreeRoot}\bin\ChildProcess.Native"
New-Item -ItemType Directory -Force $objDir | Out-Null
New-Item -ItemType Directory -Force $binDir | Out-Null

# Build Windows binaries.
if ($Rebuild) {
    try {
        Remove-Item $objDir/* -Recurse
        Remove-Item $binDir/* -Recurse
    }
    catch {}    
}

$pwsh = Join-Path $PSHOME "pwsh.exe"
$subbuildWin = Join-Path $worktreeRoot "src/ChildProcess.Native/Subbuild-win.ps1"
$subbuildWinArgs = @(
    $worktreeRoot
)
if (Test-Path Env:VCToolsInstallDir) {
    # Already within VsDevCmd
    & $pwsh $subbuildWin @subbuildWinArgs
}
else {
    $invokeCommandInVsDevCmd = Join-Path $worktreeRoot "build/Invoke-CommandInVsDevCmd.cmd"
    $vswhere = Join-Path ${Env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer/vswhere.exe"
    $vs2019 = & $vswhere -nologo -format json -latest -version "[16.0,17.0)" -requires Microsoft.VisualStudio.Workload.NativeDesktop | ConvertFrom-Json
    if ($null -eq $vs2019) {
        Write-Error "VS2019 not found."
        exit 1
    }
    $vsDevCmd = Join-Path $vs2019.installationPath "Common7/Tools/VsDevCmd.bat"

    & $invokeCommandInVsDevCmd $vsDevCmd $pwsh $subbuildWin @subbuildWinArgs
}

# Build Linux binaries.
if ($Rebuild) {
    try {
        docker volume rm $buildVolumeName 2>&1 | Out-Null
    }
    catch {}
}

try {
    docker rm $linuxContainerName 2>&1 | Out-Null
}
catch {}

docker volume create $buildVolumeName | Out-Null
docker run `
    --name $linuxContainerName `
    --mount "type=bind,readonly,source=${worktreeRoot}/src/ChildProcess.Native,target=/proj/src" `
    --mount "type=volume,src=${buildVolumeName},dst=/proj/build" `
    $linuxImageName /bin/bash /proj/src/Subbuild-linux.sh /proj

# If the container mounts and writes directly to a host directory, generated files will have 
# NTFS extended attributes (EAs) $LXUID/$LXGID/$LXMOD. There is no way to remove NTFS EAs via Win32 APIs.
# Even worse, NTFS EAs will be copied by CopyFile. (We can of course effectively remove NTFS EAs by creating a new file
# and copying only the file data to it). 
#
# Avoid mouting a host directory and do docker cp.
docker cp "${linuxContainerName}:/proj/bin/." "${worktreeRoot}/bin/ChildProcess.Native"
docker rm $linuxContainerName | Out-Null
