# CliWrap.FSharp Repository Instructions

This repository provides idiomatic F# support for [CliWrap](https://github.com/Tyrrrz/CliWrap), a library for running external command-line programs.

## Project Overview

- **Language**: F# with .NET
- **Purpose**: Provides F#-friendly wrappers and computation expressions for CliWrap
- **Key Features**: Async/Task support, pipeline computation expressions, idiomatic F# APIs

## Code Style and Formatting

- Use **4 spaces for indentation** in `.fs` files (standard F# convention, enforced by Fantomas)
- YAML files (`.yml`) use 2 spaces for indentation
- Markdown files (`.md`) use spaces for indentation (typically 2 spaces for nested lists)
- Other files use tabs for indentation by default (per `.editorconfig`)
- Run `dotnet fantomas .` or `make format` to format code before committing
- Follow the `.editorconfig` settings strictly
- F# newlines before multiline computation expressions are disabled (`fsharp_newline_before_multiline_computation_expression = false`)
- Always insert final newlines and trim trailing whitespace

## F# Conventions

- Use XML documentation comments (`/// <summary>`) for public APIs
- Prefer piping (`|>`) for composing functions
- Use meaningful parameter names that describe their purpose
- Follow functional programming principles: immutability, pure functions, and composition
- Use `seq`, `list`, or `array` types appropriately based on use case
- Prefer discriminated unions and pattern matching over exceptions where appropriate

## Testing

- Tests are located in `src/CliWrap.FSharp.Tests/`
- Use **xUnit** as the testing framework
- Use **FsCheck** for property-based testing
- Test files should be named with `Tests.fs` suffix (e.g., `CliTests.fs`)
- Use `[<Property>]` attribute for property-based tests
- Use `[<Fact>]` attribute for standard unit tests

## Building and Testing

- **Build**: `dotnet build` or `make build`
- **Test**: `dotnet test` or `make test`
- **Format**: `dotnet fantomas .` or `make format`
- **Restore dependencies**: `dotnet restore --locked-mode` (use locked mode to maintain package consistency)
- **Check formatting**: `dotnet fantomas --check src` (used in CI)

## Dependencies

- Main dependency: CliWrap (vendored in `vendor/CliWrap`)
- Dependencies are locked with `packages.lock.json` files
- Use `RestorePackagesWithLockFile` for consistent dependency versions

## CI/CD

- GitHub Actions workflow: `.github/workflows/main.yml`
- CI runs: build, test, lint, and code coverage
- Publishes to both NuGet.org and GitHub Packages on tags
- Uses Codecov for code coverage reporting

## Documentation

- Keep README.md up-to-date with usage examples
- Use XML doc comments for all public APIs
- Examples should demonstrate both `Async` and `Task` patterns
- Show both computation expression and module function styles in examples

## Security and Best Practices

- Never commit secrets or sensitive data
- Escape command-line arguments properly to prevent injection vulnerabilities
- Prefer the `args` function over `arg` to avoid manual escaping
- AOT compatibility: Trimming is supported, but full AOT requires FSharp.Core improvements

## Vendored Dependencies

- CliWrap is vendored as a git submodule in `vendor/CliWrap`
- Patches are stored in `vendor/patches/`
- Use Makefile targets for managing vendored code
