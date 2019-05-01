
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [switch]
    $RetailRelease = $false
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

function Exec {
    param(
        [parameter(Mandatory = $true)]
        [scriptblock]
        $cmd
    )

    & $cmd

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Command failed with exit code ${LASTEXITCODE}: $cmd"
    }
}

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
$slnFile = "$worktreeRoot\src\ChildProcess.sln"

$commitHash = (git rev-parse HEAD)
$versionInfo = Get-VersionInfo -CommitHash $commitHash -RetailRelease:$RetailRelease

$commonBuildOptions = Get-CommonBuildOptions -VersionInfo $versionInfo

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
