module EventStreamTests

open System
open System.Collections.Generic
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

/// Collects every event from a push-based event stream, completing when the stream does.
let private collect (source: IObservable<CommandEvent>) =
    let events = ResizeArray()

    let tcs =
        TaskCompletionSource<CommandEvent list>(TaskCreationOptions.RunContinuationsAsynchronously)

    source.Subscribe(
        { new IObserver<CommandEvent> with
            member _.OnNext e = events.Add e
            member _.OnError ex = tcs.TrySetException ex |> ignore

            member _.OnCompleted() =
                tcs.TrySetResult(List.ofSeq events) |> ignore
        }
    )
    |> ignore

    tcs.Task

/// Collects every event from a pull-based event stream.
let private collectAsync (source: IAsyncEnumerable<CommandEvent>) = task {
    let events = ResizeArray()
    use e = source.GetAsyncEnumerator()
    let mutable more = true

    while more do
        let! next = e.MoveNextAsync()

        if next then events.Add e.Current else more <- false

    return List.ofSeq events
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

/// Asserts the lifecycle of a successful run: started first, exited last, and the expected line counts between.
let private assertLifecycle lines (events: CommandEvent list) =
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

let private lines = 50

let private generateText () =
    Command(Dummy.Program.FilePath).WithArguments([ "generate text"; "--target"; "all"; "--lines"; string lines ])

let private sleep () =
    Command(Dummy.Program.FilePath).WithArguments([ "sleep"; "00:00:20" ])

let private nonAscii = "héllo wörld ✓"

let private echoAll () =
    Command(Dummy.Program.FilePath).WithArguments([ "echo"; nonAscii; "--target"; "all" ])

let private cancelled () =
    let cts = new CancellationTokenSource()
    cts.Cancel()
    cts

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
    let! events = generateText () |> Cli.observe CancellationToken.None |> collect
    assertLifecycle lines events
}

[<Fact>]
let ``Should observe with cancellation`` () = task {
    use cts = cancelled ()
    let source = sleep () |> Cli.observe cts.Token

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collect source :> Task)

    ()
}

[<Fact>]
let ``Should observe with encoding`` () = task {
    let! events =
        echoAll () |> Cli.observee Encoding.UTF8 CancellationToken.None |> collect

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ nonAscii ], s.StdErr)
}

[<Fact>]
let ``Should observe with forceful and graceful tokens`` () = task {
    let! events =
        generateText ()
        |> Cli.observef CancellationToken.None CancellationToken.None
        |> collect

    assertLifecycle lines events
}

[<Fact>]
let ``Should observe with forceful cancellation`` () = task {
    use forceful = cancelled ()
    let source = sleep () |> Cli.observef forceful.Token CancellationToken.None

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collect source :> Task)

    ()
}

// Cli.listen

[<Fact>]
let ``Should listen`` () = task {
    let! events = generateText () |> Cli.listen CancellationToken.None |> collectAsync
    assertLifecycle lines events
}

[<Fact>]
let ``Should listen with cancellation`` () = task {
    use cts = cancelled ()
    let source = sleep () |> Cli.listen cts.Token

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collectAsync source :> Task)

    ()
}

[<Fact>]
let ``Should listen with encoding`` () = task {
    let! events =
        echoAll () |> Cli.listene Encoding.UTF8 CancellationToken.None |> collectAsync

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ nonAscii ], s.StdErr)
}

[<Fact>]
let ``Should listen with forceful and graceful tokens`` () = task {
    let! events =
        generateText ()
        |> Cli.listenf CancellationToken.None CancellationToken.None
        |> collectAsync

    assertLifecycle lines events
}

[<Fact>]
let ``Should listen with forceful cancellation`` () = task {
    use forceful = cancelled ()
    let source = sleep () |> Cli.listenf forceful.Token CancellationToken.None

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collectAsync source :> Task)

    ()
}

// CommandBuilder

let private generateArgs =
    [ "generate text"; "--target"; "all"; "--lines"; string lines ]

[<Fact>]
let ``Should observe in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args generateArgs
            observe
        }
        |> collect

    assertLifecycle lines events
}

[<Fact>]
let ``Should observe with cancellation in builder`` () = task {
    use cts = cancelled ()

    let source = command Dummy.Program.FilePath {
        args [ "sleep"; "00:00:20" ]
        observe cts.Token
    }

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collect source :> Task)

    ()
}

[<Fact>]
let ``Should observe with encoding in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args [ "echo"; nonAscii; "--target"; "all" ]
            observe Encoding.UTF8 CancellationToken.None
        }
        |> collect

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ nonAscii ], s.StdErr)
}

[<Fact>]
let ``Should observe with separate encodings and tokens in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args [ "echo"; nonAscii; "--target"; "all" ]
            observe Encoding.UTF8 Encoding.Latin1 CancellationToken.None CancellationToken.None
        }
        |> collect

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ Encoding.Latin1.GetString(Encoding.UTF8.GetBytes nonAscii) ], s.StdErr)
}

[<Fact>]
let ``Should listen in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args generateArgs
            listen
        }
        |> collectAsync

    assertLifecycle lines events
}

[<Fact>]
let ``Should listen with cancellation in builder`` () = task {
    use cts = cancelled ()

    let source = command Dummy.Program.FilePath {
        args [ "sleep"; "00:00:20" ]
        listen cts.Token
    }

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () -> collectAsync source :> Task)

    ()
}

[<Fact>]
let ``Should listen with encoding in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args [ "echo"; nonAscii; "--target"; "all" ]
            listen Encoding.UTF8 CancellationToken.None
        }
        |> collectAsync

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ nonAscii ], s.StdErr)
}

[<Fact>]
let ``Should listen with separate encodings and tokens in builder`` () = task {
    let! events =
        command Dummy.Program.FilePath {
            args [ "echo"; nonAscii; "--target"; "all" ]
            listen Encoding.UTF8 Encoding.Latin1 CancellationToken.None CancellationToken.None
        }
        |> collectAsync

    let s = summarize events
    Assert.Equal<string list>([ nonAscii ], s.StdOut)
    Assert.Equal<string list>([ Encoding.Latin1.GetString(Encoding.UTF8.GetBytes nonAscii) ], s.StdErr)
}
