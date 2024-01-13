[<AutoOpen>]
module UnMango.CliWrap.FSharp.CommandBuilder

open System.Collections.Generic
open System.ComponentModel
open CliWrap

type CommandBuilder(target: string) =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(_: unit) = Command(target)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(command: Command) = command

    [<CustomOperation("env")>]
    member _.Env(command: Command, env) =
        command.WithEnvironmentVariables((dict env).AsReadOnly())

    [<CustomOperation("workingDirectory")>]
    member _.WorkingDirectory(command: Command, directory) = command.WithWorkingDirectory(directory)

    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string) = command.WithArguments(args)

    [<CustomOperation("args")>]
    member _.Args(command: Command, args: string seq) = command.WithArguments(args)

    [<CustomOperation("stderr")>]
    member _.StdErr(command: Command, pipe) = command.WithStandardErrorPipe(pipe)

    [<CustomOperation("stderr")>]
    member _.StdErr(command: Command, stream) =
        PipeTarget.ToStream(stream) |> command.WithStandardErrorPipe

    [<CustomOperation("stdin")>]
    member _.StdIn(command: Command, pipe) = command.WithStandardInputPipe(pipe)

    [<CustomOperation("stdin")>]
    member _.StdIn(command: Command, stream) =
        PipeSource.FromStream(stream) |> command.WithStandardInputPipe

    [<CustomOperation("stdout")>]
    member _.StdOut(command: Command, pipe) = command.WithStandardOutputPipe(pipe)

    [<CustomOperation("stdout")>]
    member _.StdOut(command: Command, stream) =
        PipeTarget.ToStream(stream) |> command.WithStandardOutputPipe

    [<CustomOperation("validation")>]
    member _.Validation(command: Command, validation) = command.WithValidation(validation)

let command target = CommandBuilder(target)
