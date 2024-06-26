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

Import-Module "$PSScriptRoot\psm\Build.psm1"

$worktreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$linuxImageName = "asmichi/childprocess-buildtools-ubuntu-crosssysroot:22.04.20240702.1"
$linuxContainerName = "${NamePrefix}-buildnativelib-linux"
$buildVolumeName = "${NamePrefix}-buildnativelib-linux"

$successful = $true

# Prepare the output directory.
$objDir = "${worktreeRoot}\obj\ChildProcess.Native"
$binDir = "${worktreeRoot}\bin\ChildProcess.Native"
New-Item -ItemType Directory -Force $objDir | Out-Null
New-Item -ItemType Directory -Force $binDir | Out-Null

# Build Windows binaries.
if ($Rebuild) {
    try {
        Remove-Item $objDir/* -Recurse
    }
    catch {}
}

$pwsh = Join-Path $PSHOME "pwsh.exe"
$subbuildWin = Join-Path $worktreeRoot "src/ChildProcess.Native/Subbuild-win.ps1"
$subbuildWinArgs = @(
    $worktreeRoot
)

$winJob = $null
if (Test-Path Env:VCToolsInstallDir) {
    # Already within VsDevCmd
    $winJob = Start-ThreadJob -ScriptBlock { & $using:pwsh $using:subbuildWin @using:subbuildWinArgs }
}
else {
    $invokeCommandInVsDevCmd = Join-Path $worktreeRoot "build/Invoke-CommandInVsDevCmd.cmd"
    $vsDevCmd = Get-VsDevCmdLocation
    $winJob = Start-ThreadJob -ScriptBlock { & $using:invokeCommandInVsDevCmd $using:vsDevCmd $using:pwsh $using:subbuildWin @using:subbuildWinArgs }
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
    --mount "type=volume,src=${buildVolumeName},dst=/proj/obj" `
    $linuxImageName /bin/bash /proj/src/Subbuild-unix.sh Linux /proj/obj /proj/obj/out/ChildProcess.Native

if ($LASTEXITCODE -ne 0) {
    $successful = $false
}

# If the container mounts and writes directly to a host directory, generated files will have
# NTFS extended attributes (EAs) $LXUID/$LXGID/$LXMOD. There is no way to remove NTFS EAs via Win32 APIs.
# Even worse, NTFS EAs will be copied by CopyFile. (We can of course effectively remove NTFS EAs by creating a new file
# and copying only the file data to it).
#
# Avoid mouting a host directory and do docker cp.
docker cp "${linuxContainerName}:/proj/obj/out/ChildProcess.Native/." "${worktreeRoot}/bin/ChildProcess.Native"
docker rm $linuxContainerName | Out-Null

if ($null -ne $winJob) {
    Receive-Job -Wait -AutoRemoveJob $winJob -ErrorAction Continue
    if (-not $?) {
        $successful = $false
    }
}

if (-not $successful) {
    Write-Error "*** BuildNativeLib FAILED ***"
}
