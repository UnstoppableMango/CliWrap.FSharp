/// Conformance specs ported from upstream's CliWrap.Tests/PushEventStreamSpecs.cs.
module Conformance.PushEventStreamSpecs

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

    let! events = cmd |> Cli.observe CancellationToken.None |> push (fun _ _ -> true)

    assertThousandLineEvents events
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and unsubscribe from it early`` () = task {
    let cmd = generating [ "--lines"; "1000" ]

    let! events =
        cmd |> Cli.observe CancellationToken.None |> push (fun count _ -> count < 10)

    Assert.Equal(10, events.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and not hang on large stdout and stderr`` () = task {
    let cmd = generating [ "--length"; "10000000"; "--lines"; "1000" ]

    let! _ = cmd |> Cli.observe CancellationToken.None |> push (fun _ _ -> true)
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and not hang on large stdout and stderr if I unsubscribe from it early``
    ()
    = task {
    let cmd = generating [ "--length"; "10000000"; "--lines"; "1000" ]

    let! events =
        cmd |> Cli.observe CancellationToken.None |> push (fun count _ -> count < 10)

    Assert.Equal(10, events.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it by unsubscribing from it`` () = task {
    let cmd = sleeping ()

    let! events =
        cmd
        |> Cli.observe CancellationToken.None
        |> push (fun count event ->
            match event with
            | Started _ -> false
            | _ -> notDone count event)

    let processId =
        match List.last events with
        | Started pid -> pid
        | _ -> failwith "The stream ended before the process started."

    // Unsubscribing returns at once, but the process is terminated asynchronously.
    try
        use timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5.0))
        use p = Diagnostics.Process.GetProcessById(processId)
        do! p.WaitForExitAsync(timeout.Token)
    with _ ->
        ()

    Assert.False(isRunning processId)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it immediately`` () = task {
    use cts = new CancellationTokenSource()
    do! cts.CancelAsync()

    let cmd = sleeping ()

    do! assertCanceledBy cts.Token (fun () -> cmd |> Cli.observe cts.Token |> push notDone :> Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    let cmd = sleeping ()

    do! assertCanceledBy cts.Token (fun () -> cmd |> Cli.observe cts.Token |> push notDone :> Task)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command as an event stream and cancel it gracefully after a delay`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(TimeSpan.FromSeconds(0.2))

    let cmd = sleeping ()

    do!
        assertCanceledBy cts.Token (fun () ->
            cmd |> Cli.observef CancellationToken.None cts.Token |> push notDone :> Task)
}
