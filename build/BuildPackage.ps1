
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [switch]
    $AllowRetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
. $worktreeRoot\Build\Common.ps1
$slnFile = "$worktreeRoot\src\ChildProcess.sln"

$commitHash = $(git rev-parse HEAD)
$branchName = $(git rev-parse --abbrev-ref HEAD)
$versionInfo = Get-VersionInfo -CommitHash $commitHash -BranchName $branchName -AllowRetailRelease:$AllowRetailRelease

$commonBuildOptions = Get-CommonBuildOptions -VersionInfo $versionInfo

& $worktreeRoot\Build\BuildNativeLib.ps1

Exec { dotnet restore --verbosity:quiet $slnFile }
Exec { dotnet build @commonBuildOptions $slnFile }
Exec { dotnet test @commonBuildOptions "$worktreeRoot\src\ChildProcess.Test\ChildProcess.Test.csproj" }

Exec {
    nuget pack `
        -Verbosity quiet -ForceEnglishOutput `
        -Version $($versionInfo.PackageVersion) `
        -BasePath "$worktreeRoot\bin\ChildProcess\AnyCPU\Release" `
        -OutputDirectory "$worktreeRoot\bin\nupkg" `
        -Properties commitHash=$($versionInfo.CommitHash) `
        "$worktreeRoot\build\nuspec\Asmichi.ChildProcess.nuspec"
}
