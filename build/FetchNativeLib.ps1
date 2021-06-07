# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project worktreeRoot for details.
#
# Fetch ChildProcess.Native binaries from Azure DevOps
#
# For Azure DevOps REST API, see https://docs.microsoft.com/en-us/rest/api/azure/devops/.

#Requires -Version 7.0

param(
    [parameter()]
    [string]
    $Revision,
    [parameter()]
    [string]
    $SourceRef = "refs/heads/master"
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$archiveNames = @(
    "ChildProcess.Native-linux",
    "ChildProcess.Native-osx"
    "ChildProcess.Native-win"
)

$worktreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$cacheBaseDir = Join-Path $worktreeRoot "obj/FetchedArtifacts"
$definitionId = 5
$sourceRemote = "origin"
$sourceBranchName = "master"

function Test-FetchedArchives {
    param(
        [parameter(Mandatory = $true)]
        $CacheDir
    )

    foreach ($name in $archiveNames) {
        if (-not (Test-Path (Join-Path $CacheDir "${name}.zip"))) {
            return $false
        }
    }

    return $true
}

function FetchArchive {
    param(
        [parameter(Mandatory = $true)]
        $CacheDir,
        [parameter(Mandatory = $true)]
        $BuildId
    )

    foreach ($name in $archiveNames) {
        if (-not (Test-Path (Join-Path $CacheDir $name))) {
            Write-Host "Fetching ${name}.zip..."
            $outFile = Join-Path $CacheDir "${name}.zip"
            $artifact = Invoke-RestMethod -Method Get -Uri "https://dev.azure.com/asmichi/ChildProcess/_apis/build/builds/${BuildId}/artifacts?artifactName=${name}&api-version=6.1-preview.5"
            Invoke-WebRequest -Uri $artifact.resource.downloadUrl -OutFile $outFile
        }
    }
}

if ([String]::IsNullOrEmpty($Revision)) {
    $Revision = $(git merge-base HEAD "${sourceRemote}/${sourceBranchName}")
}

$cacheDir = Join-Path $cacheBaseDir $Revision
if (Test-FetchedArchives -CacheDir $cacheDir) {
    Write-Host "Already have artifacts for $Revision."
}
else {
    New-Item $cacheDir -ItemType Directory -Force | Out-Null

    Write-Host "Fetching artifacts for '${Revision}'..."

    # https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/list?view=azure-devops-rest-6.1
    $builds = Invoke-RestMethod -Method Get -Uri "https://dev.azure.com/asmichi/ChildProcess/_apis/build/builds?definitions=${definitionId}&statusFilter=completed&resultFilter=succeeded&`$top=100&queryOrder=finishTimeDescending&branchName=${SourceRef}&api-version=6.1-preview.6"

    [array]$matchedBuilds = $builds.value | Where-Object { $_.sourceVersion -eq $Revision }

    if ($matchedBuilds.Count -eq 0) {
        Write-Error "Build not found. Rebase your branch onto origin/${sourceBranchName}. Make sure you have latest revision fetched. For the latest build see also: https://dev.azure.com/asmichi/ChildProcess/_build/latest?definitionId=${definitionId}&branchName=${sourceBranchName}"
        exit 1
    }

    # https://docs.microsoft.com/en-us/rest/api/azure/devops/build/artifacts/get%20artifact?view=azure-devops-rest-6.1
    $buildId = $matchedBuilds[0].id

    FetchArchive -CacheDir $cacheDir -BuildId $buildId
}

$temp = Join-Path $worktreeRoot "obj/TempExtractedArtifacts"
$dstBase = Join-Path $worktreeRoot "bin/ChildProcess.Native"
New-Item $dstBase -ItemType Directory -Force | Out-Null
foreach ($name in $archiveNames) {
    Write-Host "Extracting ${name}.zip..."
    $archiveName = Join-Path $cacheDir "${name}.zip"
    Expand-Archive -Force -LiteralPath $archiveName -DestinationPath $temp
    foreach ($src in (Get-ChildItem (Join-Path $temp $name))) {
        $dst = Join-Path $dstBase $src.Name
        if (Test-Path $dst) {
            Remove-Item -LiteralPath $dst -Recurse
        }
        Move-Item -LiteralPath $src.FullName -Destination $dst
        Write-Host "    Created ${dst}/$($src.Name)."
    }
}
Remove-Item -LiteralPath $temp -Recurse

Write-Host "Complete!"
exit 0
