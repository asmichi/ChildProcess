[English](README.md)

# Asmichi.ChildProcess
子プロセスを生成する機能を提供します。子プロセスを生成して操作することに関しては、`System.Diagnostics.Process` よりも簡単に使えます。また、デフォルト値がより安全であり、柔軟なリダイレクト制御も可能です。

[NuGet](https://www.nuget.org/packages/Asmichi.ChildProcess/) で取得することができます。

[![Build Status](https://dev.azure.com/asmichi/ChildProcess/_apis/build/status/ChildProcess-CI?branchName=master)](https://dev.azure.com/asmichi/ChildProcess/_build/latest?definitionId=5&branchName=master)

[Wiki](https://github.com/asmichi/ChildProcess/wiki) に本ライブラリのゴールとロードマップがあります。

## `System.Diagnostics.Process` との比較

- 子プロセスを生成してその出力を得ることに注力している。
    - プロセスの状態を取得することはできない。
    - 常駐プロセスを生成することはできない。
- より多くのリダイレクト先をサポート:
    - NUL
    - ファイル (追記も可能)
    - パイプ
    - ハンドル
- リダイレクトのデフォルト値がより安全:
    - stdin は NUL
    - stdout は現在の stdout
    - stderr は現在の stderr
- パイプは非同期である。すなわち、非同期の読み書きは IO 完了ポートが扱う。
- 子プロセスの終了を保証する。

# ライセンス

[The MIT License](LICENSE)

# サポートされるランタイム

- .NET Core 3.1 以降

RIDs:

- `win10-x86` (未テスト)
- `win10-x64` (1809 以降; 1809 でテスト済)
- `win10-arm` (未テスト)
- `win10-arm64` (未テスト)
- `linux-x64` (Ubuntu 18.04 でテスト済)
- `linux-arm` (未テスト)
- `linux-arm64` (未テスト)
- `linux-musl-arm64` (未テスト)
- `linux-musl-x64` (Alpine 3.13 でテスト済)
- `osx-x64` (macOS 10.15 Catalina 以降; 10.15 でテスト済)
- `osx-arm64` (macOS 11.0 Big Sur 以降; 未テスト)

NOTE: glibc ベースの Linux では、glibc 2.x.y 以降と libstdc++ 3.x.y 以降が必要です。

NOTE: `osx-arm64` は .NET 6で導入される予定です。[dotnet/runtime#43313](https://github.com/dotnet/runtime/issues/43313)

# Known Issues

- Windows 10 1809 (Windows Server 2019 を含む) では、 `SignalTermination` は単にプロセスツリーを強制終了します (`Kill` と同じ操作です).
    - [`ClosePseudoConsole`](https://docs.microsoft.com/en-us/windows/console/closepseudoconsole) が pseudoconsole にアタッチされたプログラムを終了しないバグがあるためです。
- 11.0 より前の macOS では、シグナルによって終了したプロセスの `ExitCode` は常に `-1` になります。
    - そのようなプロセスについて `waitid` が `siginfo_t.si_status` に `0` を返すバグがあるためです。

# 注意

- `ChildProcessCreationContext` や `ChildProcessFlags. DisableEnvironmentVariableInheritance` を使用して環境変数を完全に上書きする場合、 `SystemRoot` などの基本的な環境変数を含めることを推奨します。

# 制限事項

- 最大で 2^63 個のプロセスしか生成できません。

# ランタイムに関する仮定

本ライブラリは、ランタイムに関して以下の性質を仮定しています:

- Windows
    - `SafeFileHandle` の内部値はファイルハンドルである。
    - `SafeWaitHandle` の内部値は `WaitForSingleObject` で待つことができる。
    - `SafeProcessHandle` の内部値はプロセスハンドルである。
- *nix
    - `SafeFileHandle` の内部値はファイルディスクリプタである。
    - `SafeProcessHandle` の内部値はプロセス ID である。
    - `Socket.Handle` はソケットのファイルディスクリプタを返す。

# サンプル

追加のサンプルは [ChildProcess.Example](src/ChildProcess.Example/) を参照してください。

## 基本

子プロセスの出力を読み取ることができます。その際、 stdout と stderr を結合することもできます。

```cs
var si = new ChildProcessStartInfo("cmd", "/C", "echo", "foo")
{
    StdOutputRedirection = OutputRedirection.OutputPipe,
    // 2>&1 のような効果
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

## ファイルへのリダイレクト

子プロセスの出力をファイルにリダイレクトできます。その際、子プロセスの出力を読む必要はありません。

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

## 真のパイプ

子プロセスの出力を、ほかの子プロセスの入力にパイプできます。その際、子プロセスの出力を読む必要はありません。

```cs
// 匿名パイプを作る
using var inPipe = new AnonymousPipeServerStream(PipeDirection.In);

var si1 = new ChildProcessStartInfo("cmd", "/C", "set")
{
    // 出力をパイプの書き込み側に接続する
    StdOutputRedirection = OutputRedirection.Handle,
    StdErrorRedirection = OutputRedirection.Handle,
    StdOutputHandle = inPipe.ClientSafePipeHandle,
    StdErrorHandle = inPipe.ClientSafePipeHandle,
    Flags = ChildProcessFlags.UseCustomCodePage,
    CodePage = Encoding.Default.CodePage, // UTF-8
};

var si2 = new ChildProcessStartInfo("findstr", "Windows")
{
    // 入力をパイプの読み取り側に接続する
    StdInputRedirection = InputRedirection.Handle,
    StdInputHandle = inPipe.SafePipeHandle,
    StdOutputRedirection = OutputRedirection.OutputPipe,
    StdErrorRedirection = OutputRedirection.OutputPipe,
    Flags = ChildProcessFlags.UseCustomCodePage,
    CodePage = Encoding.Default.CodePage, // UTF-8
};

using var p1 = ChildProcess.Start(si1);
using var p2 = ChildProcess.Start(si2);

// パイプハンドルのコピーを閉じる。 (そうしないと、 p2 はパイプからの読み取りでブロックし続ける。)
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
