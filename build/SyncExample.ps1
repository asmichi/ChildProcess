
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

# Update the example using the published version (which allows users to easily run the example).

#Requires -Version 7.0

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$worktreeRoot = Resolve-Path "$PSScriptRoot\.."
$exampleDir = "$worktreeRoot\src\ChildProcess.Example"
$examplePreviewDir = "$worktreeRoot\src\ChildProcess.ExamplePreview"
$baseVersion = Get-Content "$worktreeRoot\build\Version.txt"

Get-ChildItem $exampleDir -Include "*.cs" | Remove-Item
Copy-Item "$examplePreviewDir\*.cs" $exampleDir
$csprojContent = Get-Content "$exampleDir\ChildProcess.Example.csproj"
$csprojContent = $csprojContent -creplace "Version=`".*`"","Version=`"$baseVersion`""
Set-Content "$exampleDir\ChildProcess.Example.csproj" -Value $csprojContent -Encoding utf8BOM
