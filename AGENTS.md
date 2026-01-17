# AI Agent Instructions for CliWrap.FSharp

This file provides instructions for AI agents working on the CliWrap.FSharp repository.

## Repository Context

CliWrap.FSharp is an F# wrapper library for CliWrap, providing idiomatic F# support through computation expressions and functional APIs. The library aims to make command-line program execution feel natural in F# code.

## Development Environment

**Prerequisites:**
- .NET SDK (version specified in `global.json`)
- F# 6.0 or later
- Fantomas for code formatting

**Setup:**
```bash
# Restore dependencies with locked mode
dotnet restore --locked-mode

# Build the project
dotnet build

# Run tests
dotnet test

# Format code
dotnet fantomas .
```

## Code Standards

### F# Style
- **Indentation**: 4 spaces (configured in `.editorconfig`)
- **Formatting**: Use Fantomas; run `make format` before committing
- **Documentation**: XML doc comments for all public APIs
- **Naming**: camelCase for functions/values, PascalCase for types/modules

### Functional Programming Principles
- Prefer immutability and pure functions
- Use piping (`|>`) for function composition
- Leverage computation expressions for complex workflows
- Use discriminated unions and pattern matching over exceptions

## Architecture

### Core Components
1. **Cli module** (`Cli.fs`): Provides piping-based API for building commands
2. **CommandBuilder** (`CommandBuilder.fs`): Computation expression for command building
3. **PipeBuilder** (`PipeBuilder.fs`): Computation expression for pipeline composition
4. **Pipes module** (`Pipes.fs`): Helper functions for pipe operations

### Key Design Patterns
- Builder pattern exposed through computation expressions
- Both `Async<'T>` and `Task<'T>` support for async operations
- Fluent API through piping operations
- Wrapper around CliWrap maintaining its design philosophy

## Testing Strategy

- **Unit Tests**: xUnit with `[<Fact>]` attribute
- **Property Tests**: FsCheck with `[<Property>]` attribute
- **Test Location**: `src/CliWrap.FSharp.Tests/`
- **Coverage**: Codecov integration via GitHub Actions

### Writing Tests
```fsharp
// Property-based test example
[<Property>]
let ``Should create command`` target =
    let expected = Command(target)
    let actual = Cli.wrap target
    expected.TargetFilePath = actual.TargetFilePath

// Unit test example
[<Fact>]
let ``Should configure arguments`` () =
    let cmd = Cli.wrap "dotnet"
    let actual = cmd |> Cli.args ["build"]
    Assert.NotNull(actual)
```

## Build and CI/CD

### Local Development
- `make build` - Build the solution
- `make test` - Run all tests
- `make format` - Format code with Fantomas
- `make trimmable` - Test trimming support
- `make aot` - Test AOT compilation

### CI Pipeline (`.github/workflows/main.yml`)
1. **Build**: Restore, build, test with code coverage
2. **Lint**: Check code formatting with Fantomas
3. **Package**: Create NuGet package
4. **Publish**: Deploy to NuGet.org and GitHub Packages on tags

## Dependencies

### Main Dependencies
- **CliWrap**: Vendored as submodule in `vendor/CliWrap`
- **FSharp.Core**: Standard F# library
- **xUnit**: Test framework
- **FsCheck**: Property-based testing

### Dependency Management
- Use `packages.lock.json` for deterministic builds
- Renovate bot manages dependency updates
- Submodules for vendored dependencies

## Common Tasks

### Adding New API Functions
1. Add function to appropriate module (`Cli.fs`, `CommandBuilder.fs`, etc.)
2. Add XML documentation with `<summary>`, `<param>`, and `<returns>` tags
3. Add corresponding tests in `CliTests.fs` or `CommandBuilderTests.fs`
4. Update README.md if it's a significant new feature
5. Run `make format` to format code
6. Run `make test` to verify tests pass

### Updating CliWrap Dependency
1. Update git submodule: `git submodule update --remote vendor/CliWrap`
2. Apply any patches: `make prepare_dummy`
3. Test changes thoroughly
4. Update documentation if APIs changed

### Fixing Bugs
1. Write a failing test that reproduces the bug
2. Fix the bug with minimal code changes
3. Ensure all tests pass
4. Document the fix in commit message

## Security Considerations

- **Command Injection**: Always use `args` (array) over `arg` (string) to avoid manual escaping
- **Path Traversal**: Validate working directories and file paths
- **Credential Handling**: Never log or expose credentials
- **Resource Disposal**: Use `use` for IDisposable resources

## Documentation

- **README.md**: Keep usage examples current and clear
- **XML Docs**: Required for all public APIs
- **Examples**: Provide both `Async` and `Task` patterns
- **Comments**: Only when code isn't self-explanatory

## Questions and Issues

- Review existing issues for similar problems
- Check CliWrap documentation for underlying behavior
- F# style questions: reference [F# Style Guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/)
- Test with the dummy program in `src/CliWrap.Tests.Dummy`

## Contribution Guidelines

1. Keep changes focused and minimal
2. Write tests for all new functionality
3. Update documentation for API changes
4. Run linters and formatters before committing
5. Ensure CI passes before merging
6. Follow semantic versioning for releases
