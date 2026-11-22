/// Conformance specs ported from upstream's CliWrap.Tests/ValidationSpecs.cs.
module Conformance.ValidationSpecs

open System.Threading.Tasks
open CliWrap
open CliWrap.Exceptions
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

let private failing () =
    Cli.wrap Dummy.Program.FilePath |> Cli.args [ "exit"; "1" ]

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command and get an error if it returns a non zero exit code`` () = task {
    let cmd = failing ()

    let! error =
        Assert.ThrowsAsync<CommandExecutionException>(fun () -> cmd |> Cli.exec |> Async.StartAsTask :> Task)

    Assert.Equal(1, error.ExitCode)
    Equivalence.assertEquivalent cmd error.Command
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command with buffering and get a detailed error if it returns a non zero exit code`` () =
    task {
        let cmd = failing ()

        let! error =
            Assert.ThrowsAsync<CommandExecutionException>(fun () ->
                cmd |> Cli.execBuffered |> Async.StartAsTask :> Task)

        Assert.Contains("Exit code set to 1", error.Message) // expected stderr
        Assert.Equal(1, error.ExitCode)

        // Buffered execution adds its own output pipes, which upstream's structural
        // comparison can't see; everything else must match.
        let differences =
            Equivalence.differences cmd error.Command
            |> List.except [ "StandardOutputPipe"; "StandardErrorPipe" ]

        Assert.Empty(differences)
    }

[<Fact(Timeout = 15000)>]
let ``I can execute a command without validating the exit code`` () = task {
    let cmd = failing () |> Cli.validation CommandResultValidation.None

    let! result = cmd |> Cli.exec

    Assert.Equal(1, result.ExitCode)
    Assert.False(result.IsSuccess)
}
