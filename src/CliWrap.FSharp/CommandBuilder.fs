[<AutoOpen>]
module UnMango.CliWrap.FSharp.CommandBuilder

open System.ComponentModel
open System.Text
open CliWrap
open CliWrap.Buffered

type CommandBuilder(target: string) =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(_: unit) = Cli.wrap target

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(command: Command) = command

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run<'T>(command: CommandTask<'T>) = command

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run<'T>(task: Async<'T>) = task

    [<CustomOperation("env")>]
    member _.Env(command: Command, env) = Cli.env env command

    [<CustomOperation("workingDirectory")>]
    member _.WorkingDirectory(command: Command, directory) = Cli.workDir directory command

    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string) = Cli.arg args command

    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string seq) = Cli.args args command

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

    [<CustomOperation("exec")>]
    member _.Exec(command: Command) = command.ExecuteAsync()

    [<CustomOperation("exec")>]
    member _.Exec(command: Command, cancellationToken) = command.ExecuteAsync(cancellationToken)

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command) = command.ExecuteBufferedAsync()

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command, encoding: Encoding) = command.ExecuteBufferedAsync(encoding)

    [<CustomOperation("buffered")>]
    member _.Buffered(command: Command, encoding, cancellationToken) =
        command.ExecuteBufferedAsync(encoding, cancellationToken)

    [<CustomOperation("async")>]
    member _.Async<'T>(task: CommandTask<'T>) =
        task |> CommandTask.op_Implicit |> Async.AwaitTask

    [<CustomOperation("async")>]
    member this.Async(command: Command) = this.Async(command.ExecuteAsync())

    [<CustomOperation("async")>]
    member this.Async(command: Command, cancellationToken) =
        this.Async(command.ExecuteAsync(cancellationToken))

let command target = CommandBuilder(target)
