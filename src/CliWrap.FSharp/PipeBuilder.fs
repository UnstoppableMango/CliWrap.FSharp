[<AutoOpen>]
module UnMango.CliWrap.FSharp.PipeBuilder

open System
open System.ComponentModel
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap

module private Tuple =
    let map f (a, b) = (f a, f b)

/// <summary>
/// The commands of a <c>pipeline { }</c> chain, in source order, with the targets of the last command.
/// The builder assembles it into a <see cref="T:CliWrap.Command"/> once every item is known.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
type PipeChain =
    internal
        {
            Commands: Command list
            Stdout: PipeTarget option
            Stderr: PipeTarget option
        }

/// <summary>A <see cref="T:UnMango.CliWrap.FSharp.PipeBuilder.PipeChain"/> fed by a leading source.</summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
type SourcedPipeChain =
    internal
        {
            Source: PipeSource
            Chain: PipeChain
        }

module private PipeChain =
    let ofCommands commands =
        {
            Commands = commands
            Stdout = None
            Stderr = None
        }

    let ofTargets command stdout stderr =
        {
            Commands = [ command ]
            Stdout = stdout
            Stderr = stderr
        }

    let prepend command chain =
        { chain with
            Commands = command :: chain.Commands
        }

    /// Pipes each command's stdout into the next command's stdin, then sets the last command's targets.
    let build (source: PipeSource option) chain =
        let withSource =
            match source with
            | Some source -> fun (command: Command) -> command.WithStandardInputPipe(source)
            | None -> id

        let piped =
            match chain.Commands with
            | [] -> invalidOp "A pipeline chain has at least one command."
            | first :: rest ->
                rest
                |> List.fold
                    (fun (previous: Command) (next: Command) ->
                        next.WithStandardInputPipe(PipeSource.FromCommand(previous)))
                    (withSource first)

        let withStdout =
            match chain.Stdout with
            | Some target -> piped.WithStandardOutputPipe(target)
            | None -> piped

        match chain.Stderr with
        | Some target -> withStdout.WithStandardErrorPipe(target)
        | None -> withStdout

type PipeBuilder() =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(state: Command) = state

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(state: PipeChain) = PipeChain.build None state

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(state: SourcedPipeChain) =
        PipeChain.build (Some state.Source) state.Chain

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: Command, target: PipeTarget) =
        PipeChain.ofTargets source (Some target) None

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, stream: Stream) =
        this.Combine(source, PipeTarget.ToStream(stream))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, builder: StringBuilder) =
        this.Combine(source, PipeTarget.ToStringBuilder(builder))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, f: string -> CancellationToken -> Task) =
        this.Combine(source, PipeTarget.ToDelegate(f))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, f: string -> Task) =
        this.Combine(source, PipeTarget.ToDelegate(f))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, f: string -> unit) =
        this.Combine(source, PipeTarget.ToDelegate(f))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: Command, (stdout: PipeTarget, stderr: PipeTarget)) =
        PipeChain.ofTargets source (Some stdout) (Some stderr)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, target: Stream * Stream) =
        this.Combine(source, target |> Tuple.map PipeTarget.ToStream)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, target: StringBuilder * StringBuilder) =
        this.Combine(source, target |> Tuple.map PipeTarget.ToStringBuilder)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine
        (source: Command, (stdout: string -> CancellationToken -> Task, stderr: string -> CancellationToken -> Task))
        =
        this.Combine(source, (PipeTarget.ToDelegate(stdout), PipeTarget.ToDelegate(stderr)))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, (stdout: string -> Task, stderr: string -> Task)) =
        this.Combine(source, (PipeTarget.ToDelegate(stdout), PipeTarget.ToDelegate(stderr)))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, (stdout: string -> unit, stderr: string -> unit)) =
        this.Combine(source, (PipeTarget.ToDelegate(stdout), PipeTarget.ToDelegate(stderr)))

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(_: unit, source: PipeSource) = source

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: Command, command: Command) =
        PipeChain.ofCommands [ source; command ]

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: Command, chain: PipeChain) = PipeChain.prepend source chain

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: PipeSource, chain: PipeChain) = { Source = source; Chain = chain }

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: PipeSource, command: Command) =
        this.Combine(source, PipeChain.ofCommands [ command ])

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Stream, chain: PipeChain) =
        this.Combine(PipeSource.FromStream(source), chain)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Stream, command: Command) =
        this.Combine(PipeSource.FromStream(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: ReadOnlyMemory<byte>, chain: PipeChain) =
        this.Combine(PipeSource.FromBytes(source), chain)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: ReadOnlyMemory<byte>, command: Command) =
        this.Combine(PipeSource.FromBytes(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: byte array, chain: PipeChain) =
        this.Combine(PipeSource.FromBytes(source), chain)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: byte array, command: Command) =
        this.Combine(PipeSource.FromBytes(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: string, chain: PipeChain) =
        this.Combine(PipeSource.FromString(source), chain)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: string, command: Command) =
        this.Combine(PipeSource.FromString(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: PipeSource) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: Command) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: string) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: Stream) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: byte array) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: ReadOnlyMemory<byte>) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: PipeTarget) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: StringBuilder) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(f: string -> CancellationToken -> Task) = f

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(f: string -> Task) = f

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(f: string -> unit) = f

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: PipeTarget * PipeTarget) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: Stream * Stream) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: StringBuilder * StringBuilder) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Delay f = f ()

let pipeline = PipeBuilder()
