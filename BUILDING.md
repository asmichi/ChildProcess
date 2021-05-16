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

- Set up an Ubuntu host (18.04 recommended)
    - Execute:
        ```
        apt-get install clang-10 lld-10 g++-arm-linux-gnueabihf g++-aarch64-linux-gnu ninja-build
        ```
      (See also [src\docker\childprocess-buildtools-ubuntu\Dockerfile](src\docker\childprocess-buildtools-ubuntu\Dockerfile))
    - [Download](https://cmake.org/download/) and install CMake >3.11
- copy `src\ChildProcess.Native\CMakeSettings.template.json` to `src\ChildProcess.Native\CMakeSettings.json`
    - Have `cmakeExecutable` in `CMakeSettings.json` points to the CMake executable.
- Launch Visual Studio 2019 and "Open CMake" at src\ChildProcess.Native\CMakeLists.txt.

## Building Package

```powershell
.\build\BuildPackage.ps1
```
