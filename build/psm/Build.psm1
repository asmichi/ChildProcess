
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$worktreeRoot = Resolve-Path "$PSScriptRoot\..\.."

function Get-VersionInfo {
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
        $AllowRetailRelease
    )

    $isProtectedBranch = $BranchName -eq "master" -or $BranchName -clike "release/*"
    $shortCommitHash = $CommitHash.Substring(0, 10)
    $recentTag = $(git describe --abbrev=0)
    $commitCount = $(git rev-list --count "${recentTag}..HEAD")
    $baseVersion = Get-Content "$worktreeRoot\build\Version.txt"
    $assemblyVersion = "$baseVersion.0"
    $fileVersion = $assemblyVersion
    $informationalVersion = "$fileVersion+g$shortCommitHash"
    $retailRelease = $false
    if ($AllowRetailRelease) {
        [System.Object[]]$tags = $(git tag --points-at HEAD)
        $retailRelease = $null -ne $tags -and $tags.Length -gt 0
    }
    $packageVersion = if ($retailRelease) {
        $baseVersion
    }
    elseif ($isProtectedBranch) {
        "$baseVersion-pre.$commitCount+g$shortCommitHash"
    }
    else {
        # On non-protected branches (typically feature branches), the commit hash should be included in the prerelease version
        # (not in the build metadata) so that versions from different branches will be correctly distinguished.
        "$baseVersion-pre.$commitCount.g$shortCommitHash"
    }

    return @{
        CommitHash           = $CommitHash;
        ShortCommitHash      = $shortCommitHash;
        BaseVersion          = $baseVersion;
        AssemblyVersion      = $assemblyVersion;
        FileVersion          = $fileVersion;
        InformationalVersion = $informationalVersion;
        PackageVersion       = $packageVersion;
    }
}

function Get-CommonBuildOptions {
    param
    (
        [Parameter(Mandatory = $true)]
        [Hashtable]
        $VersionInfo
    )

    return @(
        "-nologo",
        "--verbosity:quiet",
        "-p:Platform=AnyCPU",
        "--configuration",
        "Release",
        "-p:Version=$($VersionInfo.AssemblyVersion)",
        "-p:PackageVersion=$($VersionInfo.PackageVersion)",
        "-p:FileVersion=$($VersionInfo.FileVersion)",
        "-p:AssemblyVersion=$($VersionInfo.AssemblyVersion)",
        "-p:InformationalVersion=$($VersionInfo.InformationalVersion)"
    )
}

Export-ModuleMember -Function Get-CommonBuildOptions
Export-ModuleMember -Function Get-VersionInfo
