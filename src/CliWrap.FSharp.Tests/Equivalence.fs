/// Equivalence checks: compare a command built through the F# surface with the one
/// the matching upstream fluent call builds, across its whole configuration.
module Equivalence

open CliWrap
open Xunit

let private environment (command: Command) =
    command.EnvironmentVariables
    |> Seq.map (fun kv -> kv.Key, kv.Value)
    |> Seq.sortBy fst
    |> List.ofSeq

let private credentials (c: Credentials) =
    c.Domain, c.UserName, c.Password, c.LoadUserProfile

let private resourcePolicy (p: ResourcePolicy) =
    p.Priority, p.Affinity, p.MinWorkingSet, p.MaxWorkingSet

/// The names of the configuration properties that differ between two commands.
/// Pipes are compared by reference, since they carry no comparable state.
let differences (expected: Command) (actual: Command) : string list =
    [
        if expected.TargetFilePath <> actual.TargetFilePath then
            "TargetFilePath"
        if expected.Arguments <> actual.Arguments then
            "Arguments"
        if expected.WorkingDirPath <> actual.WorkingDirPath then
            "WorkingDirPath"
        if resourcePolicy expected.ResourcePolicy <> resourcePolicy actual.ResourcePolicy then
            "ResourcePolicy"
        if credentials expected.Credentials <> credentials actual.Credentials then
            "Credentials"
        if environment expected <> environment actual then
            "EnvironmentVariables"
        if expected.Validation <> actual.Validation then
            "Validation"
        if not (obj.ReferenceEquals(expected.StandardInputPipe, actual.StandardInputPipe)) then
            "StandardInputPipe"
        if not (obj.ReferenceEquals(expected.StandardOutputPipe, actual.StandardOutputPipe)) then
            "StandardOutputPipe"
        if not (obj.ReferenceEquals(expected.StandardErrorPipe, actual.StandardErrorPipe)) then
            "StandardErrorPipe"
    ]

/// Fails, naming each differing property, unless both commands have the same configuration.
let assertEquivalent (expected: Command) (actual: Command) =
    Assert.Equal<string list>([], differences expected actual)
