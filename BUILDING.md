# Building Asmichi.ChildProcess

## Environment

- Windows 10
    - WSL2 required
- PowerShell Core 7
- Visual Studio 2019
    - The Microsoft.VisualStudio.Workload.NativeDesktop workload required
- Docker for Windows
- (TBD)

## Writing and Testing code

- `pwsh .\build\BuildNativeLib.ps1`
- Open src/ChildProcess.sln with Visual Studio 2019.

In order to edit the native implementation:

- `copy src\ChildProcess.Native\CMakeSettings.template.json src\ChildProcess.Native\CMakeSettings.json`
- Open CMake at src\ChildProcess.Native\CMakeLists.txt with Visual Studio 2019.

## Building Package

```powershell
.\build\BuildPackage.ps1
```
