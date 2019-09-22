# Asmichi.ChildProcess
A .NET library that provides functionality for creating child processes. Easier, less error-prone, more flexible than `System.Diagnostics.Process` at creating child processes.

This library can be obtained via [NuGet](https://www.nuget.org/packages/Asmichi.ChildProcess/).

[![Build Status](https://dev.azure.com/asmichi/ChildProcess/_apis/build/status/ChildProcess-CI?branchName=master)](https://dev.azure.com/asmichi/ChildProcess/_build/latest?definitionId=4&branchName=master)

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
- `WaitForExitAsync`.

# License

[The MIT License](LICENSE)

# Supported Runtimes

- .NET Framework 4.7.2 or later
- .NET Core 2.1 or later

OS:

- `win10-x86` (1803 or later)
- `win10-x64` (1803 or later)
- `linux-x64` support is planned but not implemented.

# Notes

- When overriding environment variables, it is recommended that you include basic environment variables such as `SystemRoot`, etc.

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
