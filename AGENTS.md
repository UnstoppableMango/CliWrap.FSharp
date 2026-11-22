# AI Agent Instructions for CliWrap.FSharp

CliWrap.FSharp is an F# wrapper library for [CliWrap](https://github.com/Tyrrrz/CliWrap),
providing idiomatic F# support through pipe-first functions and computation expressions.

## Read these first

- **[CONTRIBUTING.md](CONTRIBUTING.md)** is the source of truth for building, testing,
  formatting, code style, the testing policy, commit messages, pull requests, and releases.
  Follow it exactly. Do not restate its rules here or in other agent files; link to it.
- **[CONTEXT.md](CONTEXT.md)** defines the project's vocabulary (Upstream, Parity,
  Conformance, ...). Use those terms in code, tests, issues, and PRs.
- **[SECURITY.md](SECURITY.md)** covers vulnerability reporting. Never discuss a suspected
  vulnerability in a public issue or PR.

## Architecture

All library code is in `src/CliWrap.FSharp/`:

1. **`Cli.fs`**: the `Cli` module, a pipe-first API for building and executing commands.
2. **`Pipes.fs`**: helper functions for pipe sources and targets.
3. **`PipeBuilder.fs`**: the `pipeline { }` computation expression.
4. **`CommandBuilder.fs`**: the `command { }` computation expression.

Tests are in `src/CliWrap.FSharp.Tests/`.
They run upstream CliWrap's Dummy program from `vendor/CliWrap`, which `make vendor` copies from the `cliwrap` flake input.
Run `make vendor` once before plain `dotnet` commands.

## Agent workflow

Before finishing a change, run the checks CONTRIBUTING.md lists: a `Release` build, the
tests, and the Fantomas formatting check. New behavior needs tests, and bug fixes need a
test that fails without the fix.

## Agent skills

### Issue tracker

Issues are tracked in this repo's GitHub Issues, via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Uses the five default triage labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
