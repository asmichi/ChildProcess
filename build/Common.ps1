
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

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
