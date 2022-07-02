# Building Asmichi.ChildProcess

## Environment

- Windows 10 or 11
    - WSL2 required
- PowerShell Core 7
- Visual Studio 2019 (only if you are modifying the native implementation)
    - The Microsoft.VisualStudio.Workload.NativeDesktop workload required
- Visual Studio 2022
- Docker for Windows (only if you are modifying the native implementation)
- (TBD)

## Writing and Testing code

- `pwsh .\build\FetchNativeLib.ps1`
- Open ChildProcess.sln with Visual Studio 2022.

In order to edit the native implementation:

- Set up an Ubuntu host (20.04 recommended)
    - Execute:
        ```
        apt-get install clang-10 lld-10 g++-arm-linux-gnueabihf g++-aarch64-linux-gnu make
        ```
      (See also [src\docker\childprocess-buildtools-ubuntu\Dockerfile](src\docker\childprocess-buildtools-ubuntu\Dockerfile))
    - [Download](https://cmake.org/download/) and install CMake >3.11
- copy `src\ChildProcess.Native\CMakeSettings.template.json` to `src\ChildProcess.Native\CMakeSettings.json`
    - Have `cmakeExecutable` in `CMakeSettings.json` points to the CMake executable.
- Launch Visual Studio 2019 and "Open CMake" at src\ChildProcess.Native\CMakeLists.txt.

## Building Native Implementation
```
pwsh .\build\BuildNativeLibWin.ps1
```
or
```
pwsh .\build\BuildNativeLibUnix.ps1
```

## Building Package

```powershell
pwsh .\build\BuildPackage.ps1
```
