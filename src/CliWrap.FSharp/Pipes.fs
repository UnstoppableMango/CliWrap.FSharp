[<AutoOpen>]
module UnMango.CliWrap.FSharp.Pipes

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open CliWrap

module PipeTo =
    let stdout = Console.OpenStandardOutput() |> PipeTarget.ToStream
    let stderr = Console.OpenStandardError() |> PipeTarget.ToStream

    let all (targets: PipeTarget list) = PipeTarget.Merge(targets)
    let string = PipeTarget.ToStringBuilder

    let stringe builder encoding =
        PipeTarget.ToStringBuilder(builder, encoding)

    let devnull = PipeTarget.Null
    let nullStream = Stream.Null |> PipeTarget.ToStream
    let f (f: string -> unit) = PipeTarget.ToDelegate(f)
    let fe encoding (f: string -> unit) = PipeTarget.ToDelegate(f, encoding)
    let fs (f: Stream -> unit) = PipeTarget.Create(f)
    let ft (f: string -> Task) = PipeTarget.ToDelegate(f)
    let ftc (f: string -> CancellationToken -> Task) = PipeTarget.ToDelegate(f)
    let ftce encoding (f: string -> CancellationToken -> Task) = PipeTarget.ToDelegate(f, encoding)
    let fte encoding (f: string -> Task) = PipeTarget.ToDelegate(f, encoding)
    let file = PipeTarget.ToFile
    let stream = PipeTarget.ToStream
    let streamf stream flush = PipeTarget.ToStream(stream, flush)
    let streamft (f: Stream -> CancellationToken -> Task) = PipeTarget.Create(f)


module ReadFrom =
    let stdin = Console.OpenStandardInput() |> PipeSource.FromStream

    let devnull = PipeSource.Null
    let nullStream = Stream.Null |> PipeSource.FromStream
    let string = PipeSource.FromString
    let stringe value encoding = PipeSource.FromString(value, encoding)
    let f (f: Stream -> unit) = PipeSource.Create(f)
    let ft (f: Stream -> CancellationToken -> Task) = PipeSource.Create(f)
    let file = PipeSource.FromFile
    let stream = PipeSource.FromStream
    let streamf stream flush = PipeSource.FromStream(stream, flush)
    let bytes (bytes: byte array) = PipeSource.FromBytes(bytes)
    let mem (memory: ReadOnlyMemory<byte>) = PipeSource.FromBytes(memory)
    let command = PipeSource.FromCommand

let inline (|>>) command source = Cli.stdin command source
