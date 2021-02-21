# Asmichi.ChildProcess
A .NET library that provides functionality for creating child processes. Easier, less error-prone, more flexible than `System.Diagnostics.Process` at creating child processes.

This library can be obtained via [NuGet](https://www.nuget.org/packages/Asmichi.ChildProcess/).

[![Build Status](https://dev.azure.com/asmichi/ChildProcess/_apis/build/status/ChildProcess-CI?branchName=master)](https://dev.azure.com/asmichi/ChildProcess/_build/latest?definitionId=5&branchName=master)

## Comparison with `System.Diagnostics.Process`

- Concentrates on creating a child process and obtaining its output.
    - Cannot query status of a process.
- More destinations of redirection:
    - NUL
    - File (optionally appended)
    - Pipe
    - Handle
- Less error-prone default values for redirection:
    - stdin to NUL
    - stdout to the current stdout
    - stderr to the current stderr
- Pipes are asynchronous; asynchronous reads and writes will be handled by IO completion ports.
- Ensures termination of child processes

# License

[The MIT License](LICENSE)

# Supported Runtimes

- .NET Core 3.1 or later

RIDs:

- `win10-x86` (Not tested)
- `win10-x64` (1803 or later)
- `win10-arm` (Not tested)
- `win10-arm64` (Not tested)
- `linux-x64`
- `linux-arm` (Not tested)
- `linux-arm64` (Not tested)

NOTE: On Linux, the system must have GLIBC 2.x.y or later and LIBSTDCXX 3.x.y or later. Musl-based Linux (Alpine, etc.) is not currently supported.

# Notes

- When overriding environment variables, it is recommended that you include basic environment variables such as `SystemRoot`, etc.

# Assumptions on Runtimes

This library assumes that the underlying runtime has the following characteristics:

- Windows
    - The inner value of a `SafeFileHandle` is a file handle.
    - The inner value of a `SafeWaitHandle` is a handle that `WaitForSingleObject` can wait for.
    - The inner value of a `SafeProcessHandle` is a process handle.
- *nix
    - The inner value of a `SafeFileHandle` is a file descriptor.
    - The inner value of a `SafeProcessHandle` is a process id.
    - `Socket.Handle` returns a socket file descriptor.

# Limitations

- More than 2^63 processes cannot be created.

# Examples

See [ChildProcess.Example](src/ChildProcess.Example/) for more examples.

## Basic

```cs
var si = new ChildProcessStartInfo("cmd", "/C", "echo", "foo")
{
    StdOutputRedirection = OutputRedirection.OutputPipe,
};

using (var p = ChildProcess.Start(si))
{
    using (var sr = new StreamReader(p.StandardOutput))
    {
        // "foo"
        Console.Write(await sr.ReadToEndAsync());
    }
    await p.WaitForExitAsync();
    // ExitCode: 0
    Console.WriteLine("ExitCode: {0}", p.ExitCode);
}
```

## Redirection to File

```cs
var si = new ChildProcessStartInfo("cmd", "/C", "set")
{
    StdOutputRedirection = OutputRedirection.File,
    StdOutputFile = "env.txt"
};

using (var p = ChildProcess.Start(si))
{
    await p.WaitForExitAsync();
}

// ALLUSERSPROFILE=C:\ProgramData
// ...
Console.WriteLine(File.ReadAllText("env.txt"));
```
