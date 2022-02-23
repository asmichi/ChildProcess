
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

# Build the package on a local environment.

#Requires -Version 7.0

param(
    [parameter()]
    [switch]
    $IncrementalBuild = $false,
    [parameter()]
    [switch]
    $AllowRetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
. $worktreeRoot\Build\Common.ps1
$slnFile = "$worktreeRoot\ChildProcess.sln"

$commitHash = $(git rev-parse HEAD)
$branchName = $(git rev-parse --abbrev-ref HEAD)
$versionInfo = Get-VersionInfo -CommitHash $commitHash -BranchName $branchName -AllowRetailRelease:$AllowRetailRelease

$commonBuildOptions = Get-CommonBuildOptions -VersionInfo $versionInfo

Exec { dotnet build @commonBuildOptions $slnFile }
Exec { dotnet test @commonBuildOptions --no-build "$worktreeRoot\src\ChildProcess.Test\ChildProcess.Test.csproj" }
Exec { dotnet pack @commonBuildOptions --no-build "$worktreeRoot\src\ChildProcess\ChildProcess.csproj" }
