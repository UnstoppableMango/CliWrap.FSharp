# Contributing to CliWrap.FSharp

Thanks for your interest in improving CliWrap.FSharp! This document is the single
source of truth for how to build, test, format, and submit changes. It applies to
human and AI contributors alike.

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE) that covers this project.

## Reporting bugs and requesting features

- **Bugs and feature requests:** open a [GitHub issue](https://github.com/UnstoppableMango/CliWrap.FSharp/issues).
  Search existing issues first. For bugs, include the package version, target
  framework, OS, and a minimal F# snippet that reproduces the problem.
- **Security vulnerabilities:** do **not** open a public issue. Follow [SECURITY.md](SECURITY.md).
- **Questions about underlying behavior:** check the
  [CliWrap documentation](https://github.com/Tyrrrz/CliWrap) first, since this library is a
  thin F# surface over it.

## Development setup

You need the .NET SDK version pinned in [`global.json`](global.json). Two optional
environments come pre-configured:

- **Dev container:** open the repository in VS Code or GitHub Codespaces.
- **Nix:** `nix develop` gives you the SDK and tools; `nix flake check` runs the Nix checks.

Clone, copy the upstream CliWrap source (this needs Nix with flakes enabled), then restore:

```bash
git clone https://github.com/UnstoppableMango/CliWrap.FSharp
cd CliWrap.FSharp
make vendor
dotnet tool restore
dotnet restore --locked-mode
```

Dependencies are pinned in `packages.lock.json` files. Restore in locked mode, and
commit lock file changes together with the `.fsproj` change that caused them.
Renovate opens dependency update PRs automatically.

## Build, test, and format

| Task                         | Command                                                  |
| ---------------------------- | -------------------------------------------------------- |
| Build                        | `make build` or `dotnet build`                           |
| Test                         | `make test` or `dotnet test`                             |
| Format                       | `dotnet fantomas src` (or `make format` with Nix)        |
| Check formatting (as CI does) | `dotnet fantomas --check src`                          |
| Check trimming support       | `make trimmable`                                         |
| Check Native AOT support     | `make aot`                                               |

CI builds and tests in `Release`. Run `dotnet build -c Release` before opening a PR
to catch anything that only shows up there.

A pre-commit hook in `.githooks/` formats staged files. Enable it with
`git config core.hooksPath .githooks`.

## Code style

Formatting is enforced by Fantomas and `.editorconfig`: 4 spaces in `.fs` files,
2 spaces in YAML, tabs elsewhere, final newline, no trailing whitespace. Beyond
formatting:

- **Public API:** every public function and type has XML doc comments (`<summary>`,
  plus `<param>`, `<returns>`, and `<exception>` where they apply). Prefer explicit
  type annotations on public signatures.
- **Naming:** `camelCase` for functions, parameters, and values; `PascalCase` for
  types, modules, and union cases. Avoid abbreviations except well-known ones like `cmd`.
- **Style:** prefer immutability, pure functions, and piping (`|>`). Prefer
  `Result`/`Option` and pattern matching over exceptions for expected errors.
- **Async:** support both `Async<'T>` and `Task<'T>` where it makes sense, and accept
  cancellation tokens for async operations.
- **Wrapping CliWrap:** keep the fluent, pipe-first feel of the `Cli` and `Pipes`
  modules. Keep computation expression builders focused and document custom operations.
  See [`CONTEXT.md`](CONTEXT.md) for the project's vocabulary.
- **Resources:** dispose `IDisposable` values with `use`.
- **Safety:** prefer `Cli.args` (a list) over `Cli.arg` (a single pre-escaped string),
  never log or expose credentials, and keep the library trimming- and AOT-friendly.

## Testing policy

**New functionality must come with automated tests, and bug fixes must come with a
test that fails without the fix.** Pull requests that change behavior without tests
will be asked to add them before merging.

Tests live in `src/CliWrap.FSharp.Tests/` and use:

- **xUnit** `[<Fact>]` for unit tests and **FsCheck** `[<Property>]` for property tests.
- Descriptive backtick names, such as ``` ``Should configure arguments`` ```.
- Arrange, act, assert, with clear names like `expected` and `actual`.
- Real implementations over mocks. For tests that run a process, use upstream's Dummy
  program (`Dummy.Program.FilePath`), built from `vendor/CliWrap/CliWrap.Tests.Dummy`.

Coverage is collected in CI and reported to Codecov. Aim for meaningful tests of the
public API and its edge cases rather than a coverage number.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/). Release notes and
the changelog are generated from commit messages on `main`, so the type matters:

| Type                                   | Use for                                | In release notes |
| -------------------------------------- | -------------------------------------- | ---------------- |
| `feat:`                                | New functionality                      | Features         |
| `fix:`                                 | Bug fixes                              | Bug Fixes        |
| `security:`                            | Fixes for a vulnerability              | Security         |
| `perf:`                                | Performance improvements               | Performance      |
| `revert:`                              | Reverting an earlier commit            | Reverts          |
| `docs:`                                | Documentation                          | Documentation    |
| `test:`, `refactor:`, `ci:`, `build:`  | Changes that don't affect users        | Hidden           |
| `chore:`, `chore(deps):`               | Maintenance and dependency bumps       | Hidden           |

Mark breaking changes with `!` (for example `feat!:`) and a `BREAKING CHANGE:` footer.
For a vulnerability fix, use the `security:` type (not `fix(security):`, which would land
under Bug Fixes) and put the advisory ID (GHSA or CVE) in the commit body. A `security:`
commit bumps the patch version and cuts a release.

## Pull requests

1. Keep each PR focused on one change, and describe what changed and why.
2. Add or update tests per the testing policy, and update `README.md` for user-facing changes.
3. Make sure the build, tests, and formatting check pass locally.
4. CI must be green before a PR is merged. It builds, tests with coverage, checks
   formatting, and validates the dev container and Nix builds.
5. The PR title should be a conventional commit message, since PRs are squash-merged.

## Upstream CliWrap source

Tests run upstream CliWrap's Dummy program, built from upstream's source at the same
release as the CliWrap package the library wraps. That source comes from the `cliwrap`
flake input in `flake.nix`, and `make vendor` copies it into the gitignored
`vendor/CliWrap`. `make build` and `make test` do this for you; plain `dotnet` commands
fail with a reminder when it is missing or out of date. Nothing from upstream is checked
in or shipped. See [ADR 0002](docs/adr/0002-upstream-source-from-flake-input.md).

When CliWrap is bumped, the `cliwrap` tag in `flake.nix` must match the new
`CliWrap` PackageReference. Renovate bumps both in one PR, and `make vendor` fails if
they differ.

## Releases

Versions follow [Semantic Versioning](https://semver.org/). Releases are cut by
[release-please](https://github.com/googleapis/release-please): it keeps a release PR
open that updates `CHANGELOG.md` from the conventional commits on `main`.

- Before 1.0, a breaking change or `feat` bumps the minor version, and `fix` or
  `security` bumps the patch. Hidden commit types don't cut a release.
- Renovate commits CliWrap bumps as `fix(deps)`, since it's the only runtime
  dependency, so they cut a release. Other dependency bumps are hidden `chore(deps)`.
- The maintainer merges the release PR by hand. Merging it creates the `v*.*.*` tag and
  GitHub Release, and the same CI run packs the package with that version and publishes
  it to NuGet.org (via trusted publishing) and GitHub Packages. Nothing is published
  from `main` otherwise.
- The current version lives in `.release-please-manifest.json`, which `flake.nix` also
  reads. Don't edit it by hand.
- Publishing stays in `ci.yml` because of trusted publishing; see
  [ADR 0001](docs/adr/0001-publish-from-ci-workflow.md).
