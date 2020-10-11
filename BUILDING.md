# Building Asmichi.ChildProcess

## Writing and Testing code

### Windows Version

In order to edit the Windows version:

- Open src/ChildProcess.sln with Visual Studio 2019.

### Linux Version

In order to edit the Linux version:

- `pwsh .\build\BuildNativeLib.ps1`
- Open src/ChildProcess.sln with Visual Studio 2019.

In order to edit the Linux native implementation:

- `copy src\ChildProcess.Native\CMakeSettings.template.json src\ChildProcess.Native\CMakeSettings.json`
- Open CMake at src\ChildProcess.Native\CMakeLists.txt with Visual Studio 2019.

## Building Package

```powershell
.\build\BuildPackage.ps1
```
