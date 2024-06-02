namespace UnMango.CliWrap.FSharp.Tests.Dummy

open System
open System.Buffers
open System.CommandLine
open System.CommandLine.Invocation
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open System.Threading.Tasks

module Opt =
    let add o (r: Command) =
        r.AddOption(o)
        r

module Cmd =
    let add c (r: Command) =
        r.AddCommand(c)
        r

    let handler (f: InvocationContext -> Task) (c: Command) =
        c.SetHandler(f)
        c

module Commands =
    let streams =
        function
        | "stdout" -> [ Console.OpenStandardOutput() ]
        | "stderr" -> [ Console.OpenStandardError() ]
        | "all" -> [ Console.OpenStandardOutput(); Console.OpenStandardError() ]
        | _ -> failwith "unsupported target"

    let rec generateBytes (rand: Random) (buf: IMemoryOwner<byte>) (streams: Stream list) len =
        function
        | total when total >= len -> ()
        | total ->
            rand.NextBytes(buf.Memory.Span)
            let wanted = Math.Min(int64 buf.Memory.Length, len - total)

            for stream in streams do
                stream.Write(buf.Memory.Slice(0, int wanted).Span)

            generateBytes rand buf streams len (total + wanted)

    let generate =
        let targetOpt = Option<string>("--target", (fun () -> "stdout"))
        let lengthOpt = Option<int64>("--length", (fun () -> 100_000L))
        let bufferOpt = Option<int>("--buffer", (fun () -> 1024))
        let rand = Random(1234567)

        Command("generate")
        |> Cmd.add (
            Command("binary")
            |> Opt.add targetOpt
            |> Opt.add lengthOpt
            |> Opt.add bufferOpt
            |> Cmd.handler (fun c -> task {
                let streams = c.ParseResult.GetValueForOption(targetOpt) |> streams
                let len = c.ParseResult.GetValueForOption(lengthOpt)
                let size = c.ParseResult.GetValueForOption(bufferOpt)
                use buf = MemoryPool<byte>.Shared.Rent(size)
                generateBytes rand buf streams len 0L
            })
        )

type Program =
    static member FilePath =
        Path.ChangeExtension(
            Assembly.GetExecutingAssembly().Location,
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                "exe"
            else
                null
        )

module Program =
    let command = RootCommand("Dummy program for testing") |> Cmd.add Commands.generate

    [<EntryPoint>]
    let main args = command.Invoke args
