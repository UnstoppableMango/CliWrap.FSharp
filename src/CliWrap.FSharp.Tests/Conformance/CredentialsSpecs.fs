/// Conformance specs ported from upstream's CliWrap.Tests/CredentialsSpecs.cs.
module Conformance.CredentialsSpecs

open System
open System.ComponentModel
open System.Threading.Tasks
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

let private onlyOnWindows () =
    Assert.SkipUnless(OperatingSystem.IsWindows(), "Starting a process as another user is only supported on Windows.")

[<Fact(Timeout = 15000)>]
let ``I can execute a command as a different user`` () = task {
    onlyOnWindows ()

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.credsf _.SetUserName("user123").SetPassword("pass123").LoadUserProfile()

    let! _ =
        Assert.ThrowsAnyAsync<Win32Exception>(fun () -> cmd |> Cli.exec |> Async.StartAsTask :> Task)

    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as a different user under the specified domain`` () = task {
    onlyOnWindows ()

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.credsf _.SetDomain("domain123").SetUserName("user123").SetPassword("pass123").LoadUserProfile()

    let! _ =
        Assert.ThrowsAnyAsync<Win32Exception>(fun () -> cmd |> Cli.exec |> Async.StartAsTask :> Task)

    ()
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command as a different user and get an error if the operating system does not support it``
    ()
    =
    Assert.SkipWhen(OperatingSystem.IsWindows(), "Starting a process as another user is fully supported on Windows.")

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.credsf _.SetUserName("user123").SetPassword("pass123")

    // Upstream awaits ExecuteAsync; with the F# surface the error surfaces when the command starts.
    Assert.Throws<NotSupportedException>(fun () -> cmd |> Cli.exec |> Async.RunSynchronously |> ignore)
    |> ignore
