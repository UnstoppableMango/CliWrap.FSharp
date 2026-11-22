/// Conformance specs ported from upstream's CliWrap.Tests/ExecutionSpecs.cs.
module Conformance.ExecutionSpecs

open System
open System.ComponentModel
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp
open Conformance.Support

let private sleeping (output: StringBuilder) =
    Cli.wrap Dummy.Program.FilePath
    |> Cli.args [ "sleep"; "00:00:20" ]
    |> Cli.stdout (PipeTo.string output)

let private assertCanceledBy (token: CancellationToken) (running: CommandTask<CommandResult>) = task {
    let! ex = Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> running.Task :> Task)
    Assert.Equal(token, ex.CancellationToken)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and get its exit code and execution time`` () = task {
    let cmd = Cli.wrap Dummy.Program.FilePath

    let! result = cmd |> Cli.exec

    Assert.Equal(0, result.ExitCode)
    Assert.True(result.IsSuccess)
    Assert.True(result.RunTime > TimeSpan.Zero)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and use an implicit conversion to get its exit code`` () = task {
    let cmd = Cli.wrap Dummy.Program.FilePath

    let! result = cmd |> Cli.exec

    Assert.Equal(0, (CommandResult.op_Implicit result: int))
    Assert.True((CommandResult.op_Implicit result: bool))
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and get its associated process ID`` () = task {
    let cmd = Cli.wrap Dummy.Program.FilePath

    let running = cmd |> Cli.Task.exec CancellationToken.None

    Assert.NotEqual(0, running.ProcessId)
    let! _ = running
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with a configured awaiter`` () = task {
    let cmd = Cli.wrap Dummy.Program.FilePath

    let! _ = (cmd |> Cli.Task.exec CancellationToken.None).ConfigureAwait(false)
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with manually configured process settings`` () = task {
    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.validation CommandResultValidation.None

    let! result =
        cmd
        |> Cli.Task.execp
            (fun startInfo -> startInfo.Arguments <- "exit 13")
            (fun proc -> proc.PriorityBoostEnabled <- false)
            CancellationToken.None
            CancellationToken.None

    Assert.Equal(13, result.ExitCode)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang on large stdout and stderr`` () = task {
    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.args [ "generate binary"; "--target"; "all"; "--length"; "100000" ]

    let! _ = cmd |> Cli.exec
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it immediately`` () = task {
    use cts = new CancellationTokenSource()
    do! cts.CancelAsync()
    let output = StringBuilder()

    let running = sleeping output |> Cli.Task.exec cts.Token

    do! assertCanceledBy cts.Token running
    Assert.False(isRunning running.ProcessId)
    Assert.DoesNotContain("Done.", output.ToString())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))
    let output = StringBuilder()

    let running = sleeping output |> Cli.Task.exec cts.Token

    do! assertCanceledBy cts.Token running
    Assert.False(isRunning running.ProcessId)
    Assert.DoesNotContain("Done.", output.ToString())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it gracefully after a delay`` () = task {
    use cts = new CancellationTokenSource()
    let output = StringBuilder()

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.args [ "sleep"; "00:00:20" ]
        |> Cli.stdout (
            PipeTo.all [
                PipeTo.f (fun line ->
                    if line.Contains("Sleeping for", StringComparison.OrdinalIgnoreCase) then
                        cts.CancelAfter(TimeSpan.FromSeconds(0.2)))
                PipeTo.string output
            ]
        )

    let running = cmd |> Cli.Task.execf CancellationToken.None cts.Token

    do! assertCanceledBy cts.Token running
    Assert.False(isRunning running.ProcessId)
    Assert.Contains("Canceled.", output.ToString())
    Assert.DoesNotContain("Done.", output.ToString())
}

[<Fact>]
let ``I can try to execute a command and get an error if the target file does not exist`` () =
    let cmd = Cli.wrap "I_do_not_exist.exe"

    // Throws synchronously: https://github.com/Tyrrrz/CliWrap/issues/139
    Assert.Throws<Win32Exception>(fun () -> cmd |> Cli.exec |> ignore) |> ignore
