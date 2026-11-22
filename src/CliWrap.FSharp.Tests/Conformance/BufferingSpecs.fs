/// Conformance specs ported from upstream's CliWrap.Tests/BufferingSpecs.cs.
module Conformance.BufferingSpecs

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap.Buffered
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

let private echo text target =
    Cli.wrap Dummy.Program.FilePath |> Cli.args [ "echo"; text; "--target"; target ]

let private sleeping () =
    Cli.wrap Dummy.Program.FilePath |> Cli.args [ "sleep"; "00:00:20" ]

let private assertCanceledBy (token: CancellationToken) (run: unit -> Task) = task {
    let! ex = Assert.ThrowsAnyAsync<OperationCanceledException>(run)
    Assert.Equal(token, ex.CancellationToken)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and get its stdout`` () = task {
    let! result = echo "Hello stdout" "stdout" |> Cli.execBuffered

    Assert.Equal("Hello stdout", result.StandardOutput.Trim())
    Assert.Empty(result.StandardError)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and get its stderr`` () = task {
    let! result = echo "Hello stderr" "stderr" |> Cli.execBuffered

    Assert.Empty(result.StandardOutput)
    Assert.Equal("Hello stderr", result.StandardError.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and get its stdout and stderr`` () = task {
    let! result = echo "Hello stdout and stderr" "all" |> Cli.execBuffered

    Assert.Equal("Hello stdout and stderr", result.StandardOutput.Trim())
    Assert.Equal("Hello stdout and stderr", result.StandardError.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and use an implicit conversion to get its stdout`` () = task {
    let! result = echo "Hello stdout" "stdout" |> Cli.execBuffered

    let stdout: string = BufferedCommandResult.op_Implicit result

    Assert.Equal("Hello stdout", stdout.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and use deconstruction to get its stdout and stderr`` () = task {
    let! result = echo "Hello stdout and stderr" "all" |> Cli.execBuffered

    let (CommandResult.Buffered(exitCode, stdout, stderr)) = result

    Assert.Equal(0, exitCode)
    Assert.Equal("Hello stdout and stderr", stdout.Trim())
    Assert.Equal("Hello stdout and stderr", stderr.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang on large stdout and stderr`` () = task {
    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.args [ "generate text"; "--target"; "all"; "--length"; "100000" ]

    let! result = cmd |> Cli.execBuffered

    Assert.False(String.IsNullOrWhiteSpace(result.StandardOutput))
    Assert.False(String.IsNullOrWhiteSpace(result.StandardError))
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it immediately`` () = task {
    use cts = new CancellationTokenSource()
    do! cts.CancelAsync()

    do! assertCanceledBy cts.Token (fun () -> (sleeping () |> Cli.Task.execBuffered cts.Token).Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    do! assertCanceledBy cts.Token (fun () -> (sleeping () |> Cli.Task.execBuffered cts.Token).Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and cancel it gracefully after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    do!
        assertCanceledBy cts.Token (fun () ->
            (sleeping ()
             |> Cli.Task.execBufferedf Encoding.Default Encoding.Default CancellationToken.None cts.Token)
                .Task)
}
