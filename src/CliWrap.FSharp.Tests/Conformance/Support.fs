/// Stand-ins for the PowerKit and Utils helpers upstream's specs use.
module Conformance.Support

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open CliWrap.EventStream
open Xunit
open UnMango.CliWrap.FSharp

/// Whether a process with this id is still running.
let isRunning (processId: int) =
    try
        use p = Process.GetProcessById(processId)
        not p.HasExited
    with
    | :? ArgumentException
    | :? InvalidOperationException -> false

/// Fails unless running the action is canceled by this token.
let assertCanceledBy (token: CancellationToken) (run: unit -> Task) = task {
    let! error = Assert.ThrowsAnyAsync<OperationCanceledException>(run)
    Assert.Equal(token, error.CancellationToken)
}

/// Splits text into its non-empty lines, like upstream's `ConsistOfLines` assertion.
let lines (text: string) =
    text.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> List.ofArray

/// Sets an environment variable for this process until the result is disposed.
let tempEnvironmentVariable (name: string) (value: string) =
    let last = Environment.GetEnvironmentVariable(name)
    Environment.SetEnvironmentVariable(name, value)

    { new IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable(name, last)
    }

/// Appends a directory to PATH until the result is disposed.
let extendPath (path: string) =
    tempEnvironmentVariable "PATH" (Environment.GetEnvironmentVariable("PATH") + $"%c{Path.PathSeparator}" + path)

/// A directory under the system temp folder that is deleted when the result is disposed.
type TempDirectory() =
    let path = Directory.CreateTempSubdirectory("CliWrap.FSharp.Tests-").FullName

    member _.Path = path

    interface IDisposable with
        member _.Dispose() =
            try
                Directory.Delete(path, true)
            with :? IOException ->
                ()

/// Pulls events until `keepGoing` returns false, which abandons the stream, or the stream ends.
/// `keepGoing` gets the number of events pulled so far, including this one.
let pull (keepGoing: int -> CommandEvent -> bool) (source: IAsyncEnumerable<CommandEvent>) = task {
    let events = ResizeArray()
    use e = source.GetAsyncEnumerator()
    let mutable more = true

    while more do
        let! next = e.MoveNextAsync()

        if next then
            events.Add e.Current
            more <- keepGoing events.Count e.Current
        else
            more <- false

    return List.ofSeq events
}

/// Collects pushed events until `keepGoing` returns false, which unsubscribes, or the stream ends.
/// `keepGoing` gets the number of events collected so far, including this one.
let push (keepGoing: int -> CommandEvent -> bool) (source: IObservable<CommandEvent>) =
    let events = ResizeArray()

    let result =
        TaskCompletionSource<CommandEvent list>(TaskCreationOptions.RunContinuationsAsynchronously)

    let subscription = ref Unchecked.defaultof<IDisposable>
    let stopped = ref false

    let stop () =
        stopped.Value <- true
        result.TrySetResult(List.ofSeq events) |> ignore

        if not (isNull subscription.Value) then
            subscription.Value.Dispose()

    subscription.Value <-
        source.Subscribe
            { new IObserver<CommandEvent> with
                member _.OnNext e =
                    if not stopped.Value then
                        events.Add e

                        if not (keepGoing events.Count e) then
                            stop ()

                member _.OnError ex = result.TrySetException ex |> ignore

                member _.OnCompleted() =
                    result.TrySetResult(List.ofSeq events) |> ignore
            }

    // An event can stop the stream before Subscribe returns the subscription.
    if stopped.Value then
        subscription.Value.Dispose()

    result.Task

/// Checks the events of a run that writes 1000 lines to each of stdout and stderr, then exits with 0.
let assertThousandLineEvents (events: CommandEvent list) =
    let pick f = events |> List.choose f

    let started =
        pick (function
            | Started pid -> Some pid
            | _ -> None)

    let stdout =
        pick (function
            | StdOut line -> Some line
            | _ -> None)

    let stderr =
        pick (function
            | StdErr line -> Some line
            | _ -> None)

    let exited =
        pick (function
            | Exited code -> Some code
            | _ -> None)

    Assert.NotEqual(0, Assert.Single(started))
    Assert.Equal(1000, stdout.Length)
    Assert.Equal(1000, stderr.Length)
    Assert.Equal(0, Assert.Single(exited))
