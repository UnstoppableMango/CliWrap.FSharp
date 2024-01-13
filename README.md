# CliWrap.FSharp

Idiomatic F# support for CliWrap

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

```fsharp
let main args = async {
	let! result = exec "dotnet" {
		args = [ "build" ]
		workingDirectory = "~/src/CliWrap.FSharp"
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

## Idiomatic? This looks nothing like normal F# code!

I've only recently been diving further into the F# ecosystem, if something looks off please open an issue!

## Why not paket?

If renovate ever [supports it](https://github.com/renovatebot/renovate/issues/11211)!
