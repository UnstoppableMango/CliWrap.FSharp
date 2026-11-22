# CliWrap.FSharp

An F#-idiomatic surface over the CliWrap .NET library for running external commands, without changing what CliWrap does.

## Language

### Wrapper

**Upstream**:
The Tyrrrz/CliWrap C# library that CliWrap.FSharp wraps.
_Avoid_: vendor, core, submodule

**Baseline release**:
The latest tagged Upstream release that CliWrap.FSharp targets for Parity (currently 3.10.5).
It is also the version of the CliWrap package the library depends on.
Untagged Upstream work does not count.
_Avoid_: prime, latest

**F# surface**:
The public API this library adds on top of Upstream: the Function layer and the Builders.
_Avoid_: bindings, F# API layer

**Function layer**:
The pipe-first functions in the `Cli` and `Pipes` modules.
This layer alone must reach Parity.
_Avoid_: core API

**Builders**:
The `command { }` and `pipeline { }` computation expressions.
They cover common paths only, so Parity does not apply to them.

**Parity**:
Every public member of the Baseline release can be reached idiomatically from the F# surface.
Parity is about API surface; matching behavior is Conformance.
_Avoid_: coverage, feature-complete

### Testing

**Conformance**:
The F# surface behaves the way Upstream does, as shown by Conformance specs.

**Conformance spec**:
A test that shows the F# surface behaves the way one of Upstream's behavioral specs says CliWrap behaves, written to go through the F# surface instead of Upstream's fluent API.
_Avoid_: upstream test, integration test

**Equivalence check**:
A test that shows a piece of the F# surface builds the same command, or gets the same result, as the matching Upstream fluent call.
_Avoid_: conformance test, parity test

**Dummy program**:
Upstream's small command-line program that Conformance specs and Equivalence checks run, built from the Upstream source of the Baseline release.
_Avoid_: dummy CLI, test CLI, fake, reference program

### Running a command

**Execution**:
Running a command once and getting a single result when it finishes (plain, buffered, or with an event stream).
_Avoid_: Run, invoke

**Event stream**:
Running a command so that its lifecycle arrives as a sequence of command events, rather than a single result.
_Avoid_: Output stream, log stream

**Observe**:
The push style of event stream: subscribers receive command events as they happen.
_Avoid_: Subscribe, watch

**Listen**:
The pull style of event stream: the caller asks for the next command event when ready.
_Avoid_: Poll, iterate, stream

**Command event**:
One step in a command's lifecycle: started (with a process id), a standard output line, a standard error line, or exited (with an exit code).
_Avoid_: Message, notification, output event

## Relationships

- Parity is done when each newly wrapped member is checked by Conformance specs.
- The Baseline release moves only when Upstream tags a new release.
