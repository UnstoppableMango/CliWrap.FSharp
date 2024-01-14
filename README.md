# CliWrap.FSharp

Idiomatic F# support for CliWrap.

## Usage

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

```fsharp
let main args =
	let cmd = pipeline {
		"an inline string source"
		Cli.wrap "echo"
	}

	cmd.ExecuteAsync()
```

## Q/A

### Idiomatic? This looks nothing like the F# I write!

If something looks off please open an issue! I've only recently been diving further into the F# ecosystem.

### Why not paket?

If renovate ever [supports it](https://github.com/renovatebot/renovate/issues/11211)!
