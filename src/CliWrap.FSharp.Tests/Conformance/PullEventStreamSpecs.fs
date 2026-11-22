/// Conformance specs ported from upstream's CliWrap.Tests/PullEventStreamSpecs.cs.
module Conformance.PullEventStreamSpecs

open System
open System.Threading
open System.Threading.Tasks
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp
open Conformance.Support

let private generating (args: string list) =
    Cli.wrap Dummy.Program.FilePath
    |> Cli.args ([ "generate text"; "--target"; "all" ] @ args)

let private sleeping () =
    Cli.wrap Dummy.Program.FilePath |> Cli.args [ "sleep"; "00:00:20" ]

let private notDone _ event =
    match event with
    | StdOut line -> Assert.DoesNotContain("Done.", line)
    | _ -> ()

    true

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream`` () = task {
    let cmd = generating [ "--lines"; "1000" ]

    let! events = cmd |> Cli.listen CancellationToken.None |> pull (fun _ _ -> true)

    assertThousandLineEvents events
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and abandon it early`` () = task {
    let cmd = generating [ "--lines"; "1000" ]

    let! events =
        cmd |> Cli.listen CancellationToken.None |> pull (fun count _ -> count < 10)

    Assert.Equal(10, events.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and not hang on large stdout and stderr`` () = task {
    let cmd = generating [ "--length"; "10000000"; "--lines"; "1000" ]

    let! _ = cmd |> Cli.listen CancellationToken.None |> pull (fun _ _ -> true)
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and not hang on large stdout and stderr if I abandon it early`` () =
    task {
        let cmd = generating [ "--length"; "10000000"; "--lines"; "1000" ]

        let! events =
            cmd |> Cli.listen CancellationToken.None |> pull (fun count _ -> count < 10)

        Assert.Equal(10, events.Length)
    }

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it by abandoning it`` () = task {
    let cmd = sleeping ()

    let! events =
        cmd
        |> Cli.listen CancellationToken.None
        |> pull (fun count event ->
            match event with
            | Started _ -> false
            | _ -> notDone count event)

    let processId =
        match List.last events with
        | Started pid -> pid
        | _ -> failwith "The stream ended before the process started."

    Assert.False(isRunning processId)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it immediately`` () = task {
    use cts = new CancellationTokenSource()
    do! cts.CancelAsync()

    let cmd = sleeping ()

    do! assertCanceledBy cts.Token (fun () -> cmd |> Cli.listen cts.Token |> pull notDone :> Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    let cmd = sleeping ()

    do! assertCanceledBy cts.Token (fun () -> cmd |> Cli.listen cts.Token |> pull notDone :> Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it gracefully after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    let cmd = sleeping ()

    do!
        assertCanceledBy cts.Token (fun () ->
            cmd |> Cli.listenf CancellationToken.None cts.Token |> pull notDone :> Task)
}
