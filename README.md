[日本語](README.ja.md)

# Asmichi.ChildProcess
A .NET library that provides functionality for creating child processes. Easier, less error-prone, more flexible than `System.Diagnostics.Process` at creating and interacting with child processes.

This library can be obtained via [NuGet](https://www.nuget.org/packages/Asmichi.ChildProcess/).

[![Build Status](https://dev.azure.com/asmichi/ChildProcess/_apis/build/status/ChildProcess-CI?branchName=master)](https://dev.azure.com/asmichi/ChildProcess/_build/latest?definitionId=5&branchName=master)

See the [Wiki](https://github.com/asmichi/ChildProcess/wiki) for the goals and the roadmap.

## Comparison with `System.Diagnostics.Process`

- Concentrates on creating a child process and obtaining its output.
    - Cannot query status of a process.
    - Cannot create a resident process.
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

- `win10-x86` (not tested)
- `win10-x64` (1809 or later; tested on 1809)
- `win10-arm` (not tested)
- `win10-arm64` (not tested)
- `linux-x64` (tested on Ubuntu 18.04)
- `linux-arm` (not tested)
- `linux-arm64` (not tested)
- `linux-musl-arm64` (not tested)
- `linux-musl-x64` (tested on Alpine 3.13)
- `osx-x64` (macOS 10.15 Catalina or later; tested on 10.15)
- `osx-arm64` (macOS 11.0 Big Sur or later; not tested)

NOTE: On glibc-based Linux, the system must have glibc 2.x.y or later and libstdc++ 3.x.y or later.

NOTE: `osx-arm64` will be introduced on .NET 6. [dotnet/runtime#43313](https://github.com/dotnet/runtime/issues/43313)

# Known Issues

- On Windows 10 1809 (including Windows Server 2019), `SignalTermination` just forcibly kills the process tree (the same operation as `Kill`).
    - This is due to a Windows pseudoconsole bug where [`ClosePseudoConsole`](https://docs.microsoft.com/en-us/windows/console/closepseudoconsole) does not terminate applications attached to the pseudoconsole.
- On macOS prior to 11.0, `ExitCode` for processes killed by a signal will always be `-1`.
    - This is due to a `waitid` bug where it returns `0` in `siginfo_t.si_status` for such processes.

# Notes

- When completely rewriting environment variables with `ChildProcessCreationContext` or `ChildProcessFlags.DisableEnvironmentVariableInheritance`, it is recommended that you include basic environment variables such as `SystemRoot`, etc.

# Limitations

- More than 2^63 processes cannot be created.

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

# Examples

See [ChildProcess.Example](src/ChildProcess.Example/) for more examples.

## Basic

You can read the output of a child, optionally combining stdout and stderr.

```cs
var si = new ChildProcessStartInfo("cmd", "/C", "echo", "foo")
{
    StdOutputRedirection = OutputRedirection.OutputPipe,
    // Works like 2>&1
    StdErrorRedirection = OutputRedirection.OutputPipe,
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

You can redirect the output of a child into a file without ever reading the output.

```cs
var si = new ChildProcessStartInfo("cmd", "/C", "set")
{
    ExtraEnvironmentVariables = new Dictionary<string, string> { { "A", "A" } },
    StdOutputRedirection = OutputRedirection.File,
    StdErrorRedirection = OutputRedirection.File,
    StdOutputFile = "env.txt",
    StdErrorFile = "env.txt",
    Flags = ChildProcessFlags.UseCustomCodePage,
    CodePage = Encoding.Default.CodePage, // UTF-8
};

using (var p = ChildProcess.Start(si))
{
    await p.WaitForExitAsync();
}

// A=A
// ALLUSERSPROFILE=C:\ProgramData
// ...
Console.WriteLine(File.ReadAllText("env.txt"));
```

## True piping

You can pipe the output of a child into another child without ever reading the output.

```cs
// Create an anonymous pipe.
using var inPipe = new AnonymousPipeServerStream(PipeDirection.In);

var si1 = new ChildProcessStartInfo("cmd", "/C", "set")
{
    // Connect the output to writer side of the pipe.
    StdOutputRedirection = OutputRedirection.Handle,
    StdErrorRedirection = OutputRedirection.Handle,
    StdOutputHandle = inPipe.ClientSafePipeHandle,
    StdErrorHandle = inPipe.ClientSafePipeHandle,
    Flags = ChildProcessFlags.UseCustomCodePage,
    CodePage = Encoding.Default.CodePage, // UTF-8 on .NET Core
};

var si2 = new ChildProcessStartInfo("findstr", "Windows")
{
    // Connect the input to the reader side of the pipe.
    StdInputRedirection = InputRedirection.Handle,
    StdInputHandle = inPipe.SafePipeHandle,
    StdOutputRedirection = OutputRedirection.OutputPipe,
    StdErrorRedirection = OutputRedirection.OutputPipe,
    Flags = ChildProcessFlags.UseCustomCodePage,
    CodePage = Encoding.Default.CodePage, // UTF-8 on .NET Core
};

using var p1 = ChildProcess.Start(si1);
using var p2 = ChildProcess.Start(si2);

// Close our copy of the pipe handles. (Otherwise p2 will get stuck while reading from the pipe.)
inPipe.DisposeLocalCopyOfClientHandle();
inPipe.Close();

using (var sr = new StreamReader(p2.StandardOutput))
{
    // ...
    // OS=Windows_NT
    // ...
    Console.Write(await sr.ReadToEndAsync());
}

await p1.WaitForExitAsync();
await p2.WaitForExitAsync();
```
