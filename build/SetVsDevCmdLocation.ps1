
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#Requires -Version 7.0

param
(
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Import-Module "$PSScriptRoot\psm\Build.psm1"

$vsDevCmd = Get-VsDevCmdLocation

Write-Host "##vso[task.setvariable variable=VSDEVCMD_LOCATION]$vsDevCmd"
