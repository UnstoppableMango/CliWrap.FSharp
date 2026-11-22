# CliWrap.FSharp

An F#-idiomatic surface over the CliWrap .NET library for running external commands.

## Language

**Upstream**:
The Tyrrrz/CliWrap C# library that CliWrap.FSharp wraps.
_Avoid_: vendor, core

**Baseline release**:
The latest tagged Upstream release that CliWrap.FSharp targets for Parity (currently 3.10.5). Untagged Upstream work does not count.
_Avoid_: prime, latest

**Parity**:
Every public member of the Baseline release can be reached idiomatically from the F# surface. Parity is about API surface; matching behavior is Conformance.
_Avoid_: coverage, feature-complete

**Function layer**:
The pipe-first functions in the `Cli` and `Pipes` modules. This layer alone must reach Parity.
_Avoid_: core API

**Builders**:
The `command { }` and `pipeline { }` computation expressions. They cover common paths only, so Parity does not apply to them.

**Conformance**:
The F# surface behaves the way Upstream does, as shown by Upstream's behavioral specs run through the F# surface.

## Relationships

- Parity is done when each newly wrapped member is checked by Conformance specs.
- The Baseline release moves only when Upstream tags a new release.
