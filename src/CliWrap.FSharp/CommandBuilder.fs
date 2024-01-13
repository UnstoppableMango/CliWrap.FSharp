[<AutoOpen>]
module UnMango.CliWrap.FSharp.CommandBuilder

open System.ComponentModel
open CliWrap

type CommandBuilder(target: string) =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(_: unit) = Cli.wrap target

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(command: Command) = command

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

let command target = CommandBuilder(target)
