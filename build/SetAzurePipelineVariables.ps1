
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#Requires -Version 7.0

param
(
    [Parameter(Mandatory = $true)]
    [string]
    $CommitHash,
    [Parameter(Mandatory = $true)]
    [string]
    $BranchName,
    [parameter()]
    [switch]
    $AllowRetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

$versionInfo = Get-VersionInfo -CommitHash $CommitHash -BranchName $BranchName -AllowRetailRelease:$AllowRetailRelease
$commonBuildOptions = Get-CommonBuildOptions -VersionInfo $versionInfo
$commonBuildOptionsString = [string]$commonBuildOptions

Write-Host "##vso[task.setvariable variable=PackageVersion]$($versionInfo.PackageVersion)"
Write-Host "##vso[task.setvariable variable=CommonBuildOptions]$commonBuildOptionsString"
Write-Host "##vso[task.setvariable variable=DOTNET_NOLOGO]1"
Write-Host "##vso[task.setvariable variable=DOTNET_CLI_TELEMETRY_OPTOUT]1"
