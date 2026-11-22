module EventStreamTests

open System
open System.Collections.Generic
open System.Diagnostics
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap
open CliWrap.EventStream
open CliWrap.Tests
open Xunit
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

type private UnknownCommandEvent() =
    inherit CommandEvent()

/// Collects every event from an observed event stream, calling onEvent for each, and completing when the stream does.
let private collectObservedWith (onEvent: CommandEvent -> unit) (source: IObservable<CommandEvent>) =
    let events = ResizeArray()

    let tcs =
        TaskCompletionSource<CommandEvent list>(TaskCreationOptions.RunContinuationsAsynchronously)

    source.Subscribe(
        { new IObserver<CommandEvent> with
            member _.OnNext e =
                onEvent e
                events.Add e

            member _.OnError ex = tcs.TrySetException ex |> ignore

            member _.OnCompleted() =
                tcs.TrySetResult(List.ofSeq events) |> ignore
        }
    )
    |> ignore

    tcs.Task

let private collectObserved = collectObservedWith ignore

/// Collects every event from a listened event stream, calling onEvent for each.
let private collectListenedWith (onEvent: CommandEvent -> unit) (source: IAsyncEnumerable<CommandEvent>) = task {
    let events = ResizeArray()
    use e = source.GetAsyncEnumerator()
    let mutable more = true

    while more do
        let! next = e.MoveNextAsync()

        if next then
            onEvent e.Current
            events.Add e.Current
        else
            more <- false

    return List.ofSeq events
}

let private collectListened = collectListenedWith ignore

/// Cancels the token source shortly after the process starts, so cancellation hits a running process.
let private cancelOnStart (cts: CancellationTokenSource) (e: CommandEvent) =
    match e with
    | Started _ -> cts.CancelAfter(100)
    | _ -> ()

/// Waits up to ten seconds for a process to exit, returning whether it did.
let private hasExited (pid: int) = task {
    try
        use p = Process.GetProcessById pid
        use timeout = new CancellationTokenSource(TimeSpan.FromSeconds 10.0)
        do! p.WaitForExitAsync(timeout.Token)
        return true
    with
    | :? ArgumentException -> return true // the process is already gone
    | :? OperationCanceledException -> return false
}

let private assertCancelled (run: unit -> Task<CommandEvent list>) = task {
    let! _ = Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> run () :> Task)
    ()
}

type private Summary =
    {
        Started: int list
        StdOut: string list
        StdErr: string list
        Exited: int list
    }

let private summarize (events: CommandEvent list) =
    let folder s e =
        match e with
        | Started pid -> { s with Started = s.Started @ [ pid ] }
        | StdOut line -> { s with StdOut = s.StdOut @ [ line ] }
        | StdErr line -> { s with StdErr = s.StdErr @ [ line ] }
        | Exited code -> { s with Exited = s.Exited @ [ code ] }

    List.fold
        folder
        {
            Started = []
            StdOut = []
            StdErr = []
            Exited = []
        }
        events

let private lines = 50

let private generateArgs =
    [ "generate text"; "--target"; "all"; "--lines"; string lines ]

let private sleepArgs = [ "sleep"; "00:00:20" ]
let private nonAscii = "héllo wörld ✓"
let private echoArgs = [ "echo"; nonAscii; "--target"; "all" ]

let private dummy (args: string list) =
    Command(Dummy.Program.FilePath).WithArguments(args)

let private cancelled () =
    let cts = new CancellationTokenSource()
    cts.Cancel()
    cts

/// Asserts the lifecycle of a successful run: started first, exited last, and the expected line counts between.
let private assertLifecycle (events: CommandEvent list) =
    Assert.True(
        (match List.head events with
         | Started _ -> true
         | _ -> false),
        "first event should be Started"
    )

    Assert.True(
        (match List.last events with
         | Exited _ -> true
         | _ -> false),
        "last event should be Exited"
    )

    let s = summarize events
    Assert.Single(s.Started) |> ignore
    Assert.NotEqual(0, List.head s.Started)
    Assert.Equal(lines, List.length s.StdOut)
    Assert.Equal(lines, List.length s.StdErr)
    Assert.Equal<int list>([ 0 ], s.Exited)

/// Asserts the echoed text was decoded with the expected encodings.
let private assertEchoed (stdout: Encoding) (stderr: Encoding) (events: CommandEvent list) =
    let decode (encoding: Encoding) =
        encoding.GetString(Encoding.UTF8.GetBytes nonAscii)

    let s = summarize events
    Assert.Equal<string list>([ decode stdout ], s.StdOut)
    Assert.Equal<string list>([ decode stderr ], s.StdErr)

// Active pattern

[<Property>]
let ``Should match started events`` (pid: int) =
    match StartedCommandEvent(pid) :> CommandEvent with
    | Started actual -> actual = pid
    | _ -> false

[<Property>]
let ``Should match standard output events`` (text: string) =
    match StandardOutputCommandEvent(text) :> CommandEvent with
    | StdOut actual -> actual = text
    | _ -> false

[<Property>]
let ``Should match standard error events`` (text: string) =
    match StandardErrorCommandEvent(text) :> CommandEvent with
    | StdErr actual -> actual = text
    | _ -> false

[<Property>]
let ``Should match exited events`` (code: int) =
    match ExitedCommandEvent(code) :> CommandEvent with
    | Exited actual -> actual = code
    | _ -> false

[<Fact>]
let ``Should reject unknown command events`` () =
    let classify (e: CommandEvent) =
        match e with
        | Started _
        | StdOut _
        | StdErr _
        | Exited _ -> ()

    Assert.Throws<ArgumentException>(fun () -> classify (UnknownCommandEvent()))
    |> ignore

// Cli.observe

[<Fact>]
let ``Should observe`` () = task {
    let! events =
        dummy generateArgs |> Cli.observe CancellationToken.None |> collectObserved

    assertLifecycle events
}

[<Fact>]
let ``Should observe with cancellation`` () = task {
    use cts = cancelled ()
    do! assertCancelled (fun () -> dummy sleepArgs |> Cli.observe cts.Token |> collectObserved)
}

[<Fact>]
let ``Should observe with cancellation while running`` () = task {
    use cts = new CancellationTokenSource()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.observe cts.Token
            |> collectObservedWith (cancelOnStart cts))
}

[<Fact>]
let ``Should stop the process when unsubscribing`` () = task {
    let started =
        TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)

    let subscription =
        dummy sleepArgs
        |> Cli.observe CancellationToken.None
        |> Observable.subscribe (fun e ->
            match e with
            | Started pid -> started.TrySetResult pid |> ignore
            | _ -> ())

    let! pid = started.Task
    subscription.Dispose()
    let! exited = hasExited pid
    Assert.True(exited, "process should exit after unsubscribing")
}

[<Fact>]
let ``Should observe with encoding`` () = task {
    let! events =
        dummy echoArgs
        |> Cli.observee Encoding.UTF8 CancellationToken.None
        |> collectObserved

    assertEchoed Encoding.UTF8 Encoding.UTF8 events
}

[<Fact>]
let ``Should observe with forceful and graceful tokens`` () = task {
    let! events =
        dummy generateArgs
        |> Cli.observef CancellationToken.None CancellationToken.None
        |> collectObserved

    assertLifecycle events
}

[<Fact>]
let ``Should observe with forceful cancellation`` () = task {
    use forceful = cancelled ()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.observef forceful.Token CancellationToken.None
            |> collectObserved)
}

[<Fact>]
let ``Should observe with graceful cancellation while running`` () = task {
    use graceful = new CancellationTokenSource()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.observef CancellationToken.None graceful.Token
            |> collectObservedWith (cancelOnStart graceful))
}

// Cli.listen

[<Fact>]
let ``Should listen`` () = task {
    let! events =
        dummy generateArgs |> Cli.listen CancellationToken.None |> collectListened

    assertLifecycle events
}

[<Fact>]
let ``Should listen with cancellation`` () = task {
    use cts = cancelled ()
    do! assertCancelled (fun () -> dummy sleepArgs |> Cli.listen cts.Token |> collectListened)
}

[<Fact>]
let ``Should listen with cancellation while running`` () = task {
    use cts = new CancellationTokenSource()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.listen cts.Token
            |> collectListenedWith (cancelOnStart cts))
}

[<Fact>]
let ``Should stop the process when abandoning the enumeration`` () = task {
    let events = dummy sleepArgs |> Cli.listen CancellationToken.None
    let e = events.GetAsyncEnumerator()
    let! _ = e.MoveNextAsync()

    let pid =
        match e.Current with
        | Started pid -> pid
        | other -> failwith $"expected Started, got {other}"

    do! e.DisposeAsync()
    let! exited = hasExited pid
    Assert.True(exited, "process should exit after abandoning the enumeration")
}

[<Fact>]
let ``Should listen with encoding`` () = task {
    let! events =
        dummy echoArgs
        |> Cli.listene Encoding.UTF8 CancellationToken.None
        |> collectListened

    assertEchoed Encoding.UTF8 Encoding.UTF8 events
}

[<Fact>]
let ``Should listen with forceful and graceful tokens`` () = task {
    let! events =
        dummy generateArgs
        |> Cli.listenf CancellationToken.None CancellationToken.None
        |> collectListened

    assertLifecycle events
}

[<Fact>]
let ``Should listen with forceful cancellation`` () = task {
    use forceful = cancelled ()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.listenf forceful.Token CancellationToken.None
            |> collectListened)
}

[<Fact>]
let ``Should listen with graceful cancellation while running`` () = task {
    use graceful = new CancellationTokenSource()

    do!
        assertCancelled (fun () ->
            dummy sleepArgs
            |> Cli.listenf CancellationToken.None graceful.Token
            |> collectListenedWith (cancelOnStart graceful))
}

// CommandBuilder

[<Fact>]
let ``Should observe in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args generateArgs
            observe
        }
        |> collectObserved

    assertLifecycle events
}

[<Fact>]
let ``Should observe with cancellation in builder`` () = task {
    use cts = cancelled ()

    do!
        assertCancelled (fun () ->
            command Dummy.Program.FilePath {
                args sleepArgs
                observe cts.Token
            }
            |> collectObserved)
}

[<Fact>]
let ``Should observe with encoding in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args echoArgs
            observe Encoding.UTF8 CancellationToken.None
        }
        |> collectObserved

    assertEchoed Encoding.UTF8 Encoding.UTF8 events
}

[<Fact>]
let ``Should observe with separate encodings and tokens in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args echoArgs
            observe Encoding.UTF8 Encoding.Latin1 CancellationToken.None CancellationToken.None
        }
        |> collectObserved

    assertEchoed Encoding.UTF8 Encoding.Latin1 events
}

[<Fact>]
let ``Should listen in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args generateArgs
            listen
        }
        |> collectListened

    assertLifecycle events
}

[<Fact>]
let ``Should listen with cancellation in builder`` () = task {
    use cts = cancelled ()

    do!
        assertCancelled (fun () ->
            command Dummy.Program.FilePath {
                args sleepArgs
                listen cts.Token
            }
            |> collectListened)
}

[<Fact>]
let ``Should listen with encoding in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args echoArgs
            listen Encoding.UTF8 CancellationToken.None
        }
        |> collectListened

    assertEchoed Encoding.UTF8 Encoding.UTF8 events
}

[<Fact>]
let ``Should listen with separate encodings and tokens in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args echoArgs
            listen Encoding.UTF8 Encoding.Latin1 CancellationToken.None CancellationToken.None
        }
        |> collectListened

    assertEchoed Encoding.UTF8 Encoding.Latin1 events
}
