/// Conformance specs ported from upstream's CliWrap.Tests/ResourcePolicySpecs.cs.
module Conformance.ResourcePolicySpecs

open System
open System.Diagnostics
open System.Threading.Tasks
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

[<Fact(Timeout = 15000)>]
let ``I can execute a command with a custom process priority`` () = task {
    Assert.SkipUnless(
        OperatingSystem.IsWindows(),
        "Starting a process with a custom priority is only supported on Windows."
    )

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.resourcePolicyf _.SetPriority(ProcessPriorityClass.High)

    let! result = cmd |> Cli.exec

    Assert.Equal(0, result.ExitCode)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with a custom core affinity`` () = task {
    Assert.SkipUnless(
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux(),
        "Starting a process with a custom core affinity is only supported on Windows and Linux."
    )

    let cmd =
        Cli.wrap Dummy.Program.FilePath |> Cli.resourcePolicyf _.SetAffinity(0b1010n) // Cores 1 and 3

    let! result = cmd |> Cli.exec

    Assert.Equal(0, result.ExitCode)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with a custom working set limit`` () = task {
    Assert.SkipUnless(
        OperatingSystem.IsWindows(),
        "Starting a process with a custom working set limit is only supported on Windows."
    )

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.resourcePolicyf _.SetMinWorkingSet(1024n * 1024n).SetMaxWorkingSet(1024n * 1024n * 10n)

    let! result = cmd |> Cli.exec

    Assert.Equal(0, result.ExitCode)
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command with a custom resource policy and get an error if the operating system does not support it``
    ()
    = task {
    Assert.SkipWhen(
        OperatingSystem.IsWindows(),
        "Starting a process with a custom resource policy is fully supported on Windows."
    )

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.resourcePolicyf _.SetMinWorkingSet(1024n * 1024n)

    let! _ =
        Assert.ThrowsAnyAsync<NotSupportedException>(fun () ->
            async { return! cmd |> Cli.exec } |> Async.StartAsTask :> Task)

    ()
}
