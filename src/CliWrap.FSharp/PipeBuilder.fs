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

type PipeBuilder() =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(state: Command) = state

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Combine(source: Command, target: PipeTarget) = source.WithStandardErrorPipe(target)

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
    member this.Combine(source: Command, (stdout: PipeTarget, stderr: PipeTarget)) =
        source.WithStandardOutputPipe(stdout).WithStandardErrorPipe(stderr)

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
    member _.Combine(source: PipeSource, command: Command) = command.WithStandardInputPipe(source)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Stream, command: Command) =
        this.Combine(PipeSource.FromStream(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: ReadOnlyMemory<byte>, command: Command) =
        this.Combine(PipeSource.FromBytes(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: byte array, command: Command) =
        this.Combine(PipeSource.FromBytes(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source, command: Command) =
        this.Combine(PipeSource.FromString(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member this.Combine(source: Command, command: Command) =
        this.Combine(PipeSource.FromCommand(source), command)

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: PipeSource) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: Command) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: string) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: Stream) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(x: StringBuilder) = x

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Delay f = f ()

let pipeline = PipeBuilder()
