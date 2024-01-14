# CliWrap.FSharp

[![Build](https://img.shields.io/github/actions/workflow/status/UnstoppableMango/CliWrap.FSharp/main.yml?branch=main)](https://github.com/UnstoppableMango/CliWrap.FSharp/actions)
[![Codecov](https://img.shields.io/codecov/c/github/UnstoppableMango/CliWrap.FSharp)](https://app.codecov.io/gh/UnstoppableMango/CliWrap.FSharp)
[![GitHub Release](https://img.shields.io/github/v/release/UnstoppableMango/CliWrap.FSharp)](https://github.com/UnstoppableMango/CliWrap.FSharp/releases)
[![NuGet Version](https://img.shields.io/nuget/v/UnMango.CliWrap.FSharp)](https://nuget.org/packages/UnMango.CliWrap.FSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/UnMango.CliWrap.FSharp)](https://nuget.org/packages/UnMango.CliWrap.FSharp)

Idiomatic F# support for [CliWrap](https://github.com/Tyrrrz/CliWrap).

## Install

- [NuGet](https://nuget.org/packages/UnMango.CliWrap.FSharp): `dotnet add package UnMango.CliWrap.FSharp`
- [GitHub Packages](): `dotnet add package UnMango.CliWrap.FSharp -s github`
  - [Authenticating to GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages)
  - [Installing from GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#installing-a-package)

## Usage

Bindings for normal CliWrap commands are available in the `Cli` module.

```fsharp
let main args = async {
    let! result =
        Cli.wrap "dotnet"
        |> Cli.args [ "build" ]
        |> Cli.workDir "~/src/CliWrap.FSharp"
        |> Cli.exec

    result.ExitCode
}
```

Cancellation token overloads are available in `Cli.Tasks`.

```fsharp
let main args = task {
    use cts = new CancellationTokenSource()
    let! result =
        Cli.wrap "dotnet"
        |> Cli.args [ "build" ]
        |> Cli.workDir "~/src/CliWrap.FSharp"
        |> Cli.Task.exec cts.Token

    result.ExitCode
}
```

```fsharp
let main args = task {
    use graceful = new CancellationTokenSource()
    use forceful = new CancellationTokenSource()
    let! result =
        Cli.wrap "dotnet"
        |> Cli.args [ "build" ]
        |> Cli.workDir "~/src/CliWrap.FSharp"
        |> Cli.Task.execf forceful.Token graceful.Token

    result.ExitCode
}
```

The core of the package is a simple computation expression that wraps `CliWrap.Command`.
It attempts to mimic the builder pattern and `.With*` style methods.

```fsharp
let main args =
  let cmd = command "dotnet" {
    args = [ "build" ]
    workingDirectory = "~/src/CliWrap.FSharp"
  }

  cmd.ExecuteAsync()
```

The computation expression also supports executing the command with `exec`.

```fsharp
let main args = task {
  let! result = command "dotnet" {
    args = [ "build" ]
    workingDirectory = "~/src/CliWrap.FSharp"
    exec
  }

  result.ExitCode
}
```

Cancellation is also supported.

```fsharp
let main args = task {
  use cts = new CancellationTokenSource()
  let! result = command "dotnet" {
    args = [ "build" ]
    workingDirectory = "~/src/CliWrap.FSharp"
    exec cts.Token
  }

  result.ExitCode
}
```

CliWrap's buffered execution is supported with `buffered`.

```fsharp
let main args = task {
  use cts = new CancellationTokenSource()
  let! result = command "dotnet" {
    args = [ "build" ]
    workingDirectory = "~/src/CliWrap.FSharp"
    buffered Encoding.UTF8 cts.Token
  }

  result.ExitCode
}
```

Asynchrony with F#'s `Async<'T>` is supported with `async`.

```fsharp
let main args = async {
  use cts = new CancellationTokenSource()
  let! result = command "dotnet" {
    args = [ "build" ]
    workingDirectory = "~/src/CliWrap.FSharp"
    async cts.Token
  }

  result.ExitCode
}
```

CliWrap's piping functionality is supported via the `pipeline` computation expression.

```fsharp
let main args =
  let cmd = pipeline {
    "an inline string source"
    Cli.wrap "echo"
  }

  cmd.ExecuteAsync()
```

Limited support for piping is available with `|>>`.

```fsharp
let main args =
  let cmd = Cli.wrap "yes" |>> Cli.wrap "echo"

  cmd.ExecuteAsync()
```

## Inspirations

The idea to abuse F# computation expressions was inspired by [Akkling.Hocon](https://github.com/Horusiath/Akkling/tree/master/src/Akkling.Hocon) and [FsHttp](https://github.com/fsprojects/FsHttp).

Obviously CliWrap was a big inspiration for this package.

## Q/A

### Idiomatic? This looks nothing like the F# I write!

If something looks off please open an issue! I've only recently been diving further into the F# ecosystem.

### Why not paket?

If renovate ever [supports it](https://github.com/renovatebot/renovate/issues/11211)!
