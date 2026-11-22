/// Equivalence checks over a command's whole configuration.
module Equivalence

open CliWrap
open Xunit

/// The environment variables as name and value pairs, sorted by name.
let environmentValues (command: ICommandConfiguration) =
    command.EnvironmentVariables
    |> Seq.map (fun variable -> variable.Key, variable.Value)
    |> Seq.sortBy fst
    |> List.ofSeq

let credentialValues (credentials: Credentials) =
    credentials.Domain, credentials.UserName, credentials.Password, credentials.LoadUserProfile

let resourcePolicyValues (policy: ResourcePolicy) =
    policy.Priority, policy.Affinity, policy.MinWorkingSet, policy.MaxWorkingSet

/// The configuration properties that differ, comparing pipes by reference.
let differences (expected: ICommandConfiguration) (actual: ICommandConfiguration) : string list =
    [
        if expected.TargetFilePath <> actual.TargetFilePath then
            "TargetFilePath"
        if expected.Arguments <> actual.Arguments then
            "Arguments"
        if expected.WorkingDirPath <> actual.WorkingDirPath then
            "WorkingDirPath"
        if
            resourcePolicyValues expected.ResourcePolicy
            <> resourcePolicyValues actual.ResourcePolicy
        then
            "ResourcePolicy"
        if credentialValues expected.Credentials <> credentialValues actual.Credentials then
            "Credentials"
        if environmentValues expected <> environmentValues actual then
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
let assertEquivalent (expected: ICommandConfiguration) (actual: ICommandConfiguration) =
    Assert.Equal<string list>([], differences expected actual)
