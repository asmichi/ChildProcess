# Building Asmichi.ChildProcess

## Author's Environments

- Windows 10 1809
    - .NET Core SDK 2.2.106
    - .NET Framework SDKs and Targeting Packs: 4.7.1, 4.7.2
    - nuget.exe 4.9.4 or later

- Ubuntu 18.04
    - `apt-get install make gcc`
    - .NET Core SDK 2.2.106
        - See https://www.microsoft.com/net/download/linux-package-manager/ubuntu18-04/sdk-current for installation instructions.

## Writing and Testing code

### Windows Version

Just open src/ChildProcess.sln.

### Linux Version

For the Linux version, (TBD)

## Building Package

```powershell
.\build\BuildPackages.ps1
```
