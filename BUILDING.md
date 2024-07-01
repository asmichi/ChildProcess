# Building Asmichi.ChildProcess

## Environment

- Windows 10 or 11
    - WSL2 required
- PowerShell Core 7
- Visual Studio 2019 (only if you are modifying the native implementation)
    - The Microsoft.VisualStudio.Workload.NativeDesktop workload required
- Visual Studio 2022
- Docker for Windows, Linux containers mode (only if you are modifying the native implementation)

## Writing and Testing code

- `pwsh .\build\FetchNativeLib.ps1`
- Open ChildProcess.sln with Visual Studio 2022.

In order to edit the native implementation:

- Set up an Ubuntu host (22.04 recommended)
    - Execute:
        ```
        apt-get install clang lld make cmake
        ```
      (See also [src\docker\childprocess-buildtools-ubuntu\Dockerfile](src\docker\childprocess-buildtools-ubuntu\Dockerfile))
- copy `src\ChildProcess.Native\CMakeSettings.template.json` to `src\ChildProcess.Native\CMakeSettings.json`
- Launch Visual Studio 2019 and "Open CMake" at src\ChildProcess.Native\CMakeLists.txt.

## Building Native Implementation

On Windows:
```
pwsh .\build\BuildNativeLibWin.ps1
```

On macOS:
```
pwsh .\build\BuildNativeLibUnix.ps1
```


## Building Package

On Windows:

```powershell
pwsh .\build\BuildPackage.ps1
```
