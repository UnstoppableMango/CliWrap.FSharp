# F# analyzers, run from a restore-only project, are the static analysis gate

The OpenSSF Best Practices badge requires a static analysis tool, and the usual choices don't cover F#: CodeQL has no F# support, `EnableNETAnalyzers` (Roslyn) only analyzes C# and VB, and Fantomas only formats.
We run the G-Research and Ionide analyzers through the `fsharp-analyzers` CLI on every CI build, treat every finding as an error, and upload SARIF to GitHub code scanning.
On top of that, Release builds treat compiler warnings as errors.

The analyzer packages live in `.config/analyzers/Analyzers.csproj`, a project that is only restored.
It is never built and isn't in the solution, so it doesn't sit in `CliWrap.FSharp.fsproj` with `PrivateAssets`.
That keeps them out of the library's lock file and the Nix build's `deps.json`.
Only the CLI tool goes in `deps.json`, because the Nix build restores the whole tool manifest.
The `FSharp.Analyzers.Build` MSBuild target was also rejected, because it ignores the analyzer's exit code and so can't fail CI.

## Considered Options

- **FSharpLint**: mostly style and naming rules, which overlap with Fantomas, so it finds few bugs.
- **Compiler warnings only**: weak evidence for the badge, and it misses analyzer rules such as untyped interpolation holes.

## Consequences

The analyzer CLI pins its own FSharp.Compiler.Service.
A new F# language feature can therefore reach the SDK before the analyzers can parse it, and when that happens, analyzer upgrades gate language upgrades.
Renovate updates the tool and the analyzer packages separately, so their SDK versions have to be bumped together.
