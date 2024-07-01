# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project worktreeRoot for details.

#Requires -Version 7.0

# TODO: Embed the commit hash.

param(
    [switch]
    [bool]
    $Rebuild = $false,
    [parameter()]
    [string]
    $OS = "OSX"
)

Set-StrictMode -Version latest

Import-Module "$PSScriptRoot/psm/Build.psm1"

$worktreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not (@("OSX") -contains $OS)) {
    Write-Error "Unknown OS: ${OS}"
    exit 1
}

# Prepare the output directory.
$objDir = "${worktreeRoot}/obj/ChildProcess.Native/Subbuild-${OS}"
$binDir = "${worktreeRoot}/bin/ChildProcess.Native"
New-Item -ItemType Directory -Force $objDir | Out-Null
New-Item -ItemType Directory -Force $binDir | Out-Null

if ($Rebuild) {
    try {
        Remove-Item $objDir/* -Recurse
    }
    catch {}
}

$subbuildUnix = Join-Path $worktreeRoot "src/ChildProcess.Native/Subbuild-unix.sh"
bash $subbuildUnix OSX ${objdir} $binDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "*** BuildNativeLib FAILED ***"
    exit 1
}

exit 0
