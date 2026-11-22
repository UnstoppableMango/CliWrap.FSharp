[<AutoOpen>]
module UnMango.CliWrap.FSharp.CommandBuilder

open System
open System.Collections.Generic
open System.ComponentModel
open System.Text
open System.Threading
open CliWrap
open CliWrap.Buffered
open CliWrap.EventStream

type CommandBuilder(target: string) =
    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string) = Cli.arg args command

    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string seq) = Cli.args args command

    [<CustomOperation("async")>]
    member _.Async<'T>(task: CommandTask<'T>) =
        task |> CommandTask.op_Implicit |> Async.AwaitTask

    [<CustomOperation("async")>]
    member this.Async(command: Command) = this.Async(command.ExecuteAsync())

    [<CustomOperation("async")>]
    member this.Async(command: Command, cancellationToken) =
        this.Async(command.ExecuteAsync(cancellationToken))

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command) = command.ExecuteBufferedAsync()

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command, encoding: Encoding) = command.ExecuteBufferedAsync(encoding)

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command, encoding, cancellationToken) =
        command.ExecuteBufferedAsync(encoding, cancellationToken)

    [<CustomOperation("env")>]
    member _.Env(command: Command, env) = Cli.env env command

    [<CustomOperation("exec")>]
    member _.Exec(command: Command) = command.ExecuteAsync()

    [<CustomOperation("exec")>]
    member _.Exec(command: Command, cancellationToken) = command.ExecuteAsync(cancellationToken)

    [<CustomOperation("listen")>]
    member _.Listen(command: Command) = command.ListenAsync()

    [<CustomOperation("listen")>]
    member _.Listen(command: Command, cancellationToken: CancellationToken) = Cli.listen cancellationToken command

    [<CustomOperation("listen")>]
    member _.Listen(command: Command, encoding: Encoding, cancellationToken: CancellationToken) =
        Cli.listene encoding cancellationToken command

    [<CustomOperation("listen")>]
    member _.Listen
        (
            command: Command,
            standardOutputEncoding: Encoding,
            standardErrorEncoding: Encoding,
            forceful: CancellationToken,
            graceful: CancellationToken
        ) =
        command.ListenAsync(standardOutputEncoding, standardErrorEncoding, forceful, graceful)

    [<CustomOperation("observe")>]
    member _.Observe(command: Command) = command.Observe()

    [<CustomOperation("observe")>]
    member _.Observe(command: Command, cancellationToken: CancellationToken) = Cli.observe cancellationToken command

    [<CustomOperation("observe")>]
    member _.Observe(command: Command, encoding: Encoding, cancellationToken: CancellationToken) =
        Cli.observee encoding cancellationToken command

    [<CustomOperation("observe")>]
    member _.Observe
        (
            command: Command,
            standardOutputEncoding: Encoding,
            standardErrorEncoding: Encoding,
            forceful: CancellationToken,
            graceful: CancellationToken
        ) =
        command.Observe(standardOutputEncoding, standardErrorEncoding, forceful, graceful)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(command: Command) = command

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run<'T>(command: CommandTask<'T>) = command

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run<'T>(task: Async<'T>) = task

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(events: IObservable<CommandEvent>) = events

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(events: IAsyncEnumerable<CommandEvent>) = events

    [<CustomOperation("stderr")>]
    member _.StdErr(command: Command, pipe) = Cli.stderr pipe command

    [<CustomOperation("stderr")>]
    member _.StdErr(command: Command, stream) =
        Cli.stderr (PipeTarget.ToStream(stream)) command

    [<CustomOperation("stdin")>]
    member _.StdIn(command: Command, pipe) = Cli.stdin pipe command

    [<CustomOperation("stdin")>]
    member _.StdIn(command: Command, stream) =
        Cli.stdin (PipeSource.FromStream(stream)) command

    [<CustomOperation("stdout")>]
    member _.StdOut(command: Command, pipe) = Cli.stdout pipe command

    [<CustomOperation("stdout")>]
    member _.StdOut(command: Command, stream) =
        Cli.stdout (PipeTarget.ToStream(stream)) command

    [<CustomOperation("validation")>]
    member _.Validation(command: Command, validation) = Cli.validation validation command

    [<CustomOperation("workingDirectory")>]
    member _.WorkingDirectory(command: Command, directory) = Cli.workDir directory command

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(_: unit) = Cli.wrap target

let command target = CommandBuilder(target)
