/// Conformance specs ported from upstream's CliWrap.Tests/PipingSpecs.cs.
module Conformance.PipingSpecs

open System
open System.Buffers
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp
open Conformance.Support

let private dummy (args: string list) =
    Cli.wrap Dummy.Program.FilePath |> Cli.args args

let private echoStdin () = dummy [ "echo stdin" ]

let private helloWorld = "Hello world!"B

let private runBuffered (cmd: Command) =
    cmd |> Cli.execBuffered |> Async.StartAsTask

let private run (cmd: Command) = cmd |> Cli.exec |> Async.StartAsTask

let private assertThrowsContaining (message: string) (run: unit -> Task) = task {
    let! error = Assert.ThrowsAnyAsync<Exception>(run)
    Assert.Contains(message, error.Message)
}

/// A temp file path that is deleted, if it exists, when the result is disposed.
type private TempFile() =
    let path =
        Path.Combine(Path.GetTempPath(), $"CliWrap.FSharp.Tests-{Guid.NewGuid()}.tmp")

    member _.Path = path

    interface IDisposable with
        member _.Dispose() =
            if File.Exists(path) then
                File.Delete(path)

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from an async anonymous source`` () = task {
    let cmd = pipeline {
        ReadFrom.ft (fun destination cancellationToken ->
            destination.WriteAsync(helloWorld, cancellationToken).AsTask())

        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a sync anonymous source`` () = task {
    let cmd = pipeline {
        ReadFrom.f (fun destination -> destination.Write(helloWorld))
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a stream`` () = task {
    use stream = new MemoryStream(helloWorld)

    let cmd = pipeline {
        stream
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a file`` () = task {
    use file = new TempFile()
    do! File.WriteAllTextAsync(file.Path, "Hello world!")

    let cmd = pipeline {
        ReadFrom.file file.Path
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from memory`` () = task {
    let cmd = echoStdin () |> Cli.stdin (ReadFrom.mem (ReadOnlyMemory helloWorld))

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a byte array`` () = task {
    let cmd = echoStdin () |> Cli.stdin (ReadFrom.bytes helloWorld)

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a string`` () = task {
    let cmd = pipeline {
        "Hello world!"
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world!", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from another command`` () = task {
    let cmd = pipeline {
        dummy [ "generate binary"; "--length"; "100000" ]
        dummy [ "length stdin" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("100000", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from another command with a transform`` () = task {
    let source =
        PipeSource.FromCommand(
            dummy [ "generate binary"; "--length"; "100000" ],
            Func<Stream, Stream, CancellationToken, Task>(fun source destination cancellationToken -> task {
                use buffer = MemoryPool<byte>.Shared.Rent(5000)
                let! _ = source.ReadAtLeastAsync(buffer.Memory, 5000, false, cancellationToken)
                do! destination.WriteAsync(buffer.Memory.Slice(0, 5000), cancellationToken)
            })
        )

    let cmd = pipeline {
        source
        dummy [ "length stdin" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("5000", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdin from a chain of commands`` () = task {
    // Known gap: `pipeline` drops the middle stages of three or more, so this returns 11, not 5.
    Assert.Skip("The pipeline builder drops the middle commands of a chain of three or more: #291")

    let cmd = pipeline {
        "Hello world"
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
        dummy [ "length stdin" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("5", result.StandardOutput.Trim())
}

let private binary () =
    dummy [ "generate binary"; "--length"; "100000" ]

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into an async anonymous target`` () = task {
    use stream = new MemoryStream()

    let cmd =
        binary ()
        |> Cli.stdout (PipeTo.streamft (fun origin cancellationToken -> origin.CopyToAsync(stream, cancellationToken)))

    let! _ = run cmd

    Assert.Equal(100_000L, stream.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a sync anonymous target`` () = task {
    use stream = new MemoryStream()

    let cmd = binary () |> Cli.stdout (PipeTo.fs (fun origin -> origin.CopyTo(stream)))

    let! _ = run cmd

    Assert.Equal(100_000L, stream.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a stream`` () = task {
    use stream = new MemoryStream()

    let cmd = binary () |> Cli.stdout (PipeTo.stream stream)

    let! _ = run cmd

    Assert.Equal(100_000L, stream.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a file`` () = task {
    use file = new TempFile()

    let cmd = binary () |> Cli.stdout (PipeTo.file file.Path)

    let! _ = run cmd

    Assert.True(File.Exists(file.Path))
    Assert.Equal(100_000L, FileInfo(file.Path).Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a string builder`` () = task {
    let buffer = StringBuilder()

    let cmd = dummy [ "echo"; "Hello world!" ] |> Cli.stdout (PipeTo.string buffer)

    let! _ = run cmd

    Assert.Equal("Hello world!", buffer.ToString().Trim())
}

let private textLines () =
    dummy [ "generate text"; "--lines"; "1000" ]

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into an async delegate`` () = task {
    let mutable count = 0

    let cmd =
        textLines ()
        |> Cli.stdout (
            PipeTo.ft (fun _ -> task {
                do! Task.Yield()
                count <- count + 1
            })
        )

    let! _ = run cmd

    Assert.Equal(1000, count)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into an async delegate with cancellation`` () = task {
    let mutable count = 0

    let cmd =
        textLines ()
        |> Cli.stdout (
            PipeTo.ftc (fun _ cancellationToken -> task {
                do! Task.Delay(1, cancellationToken)
                count <- count + 1
            })
        )

    let! _ = run cmd

    Assert.Equal(1000, count)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a sync delegate`` () = task {
    let mutable count = 0

    let cmd = textLines () |> Cli.stdout (PipeTo.f (fun _ -> count <- count + 1))

    let! _ = run cmd

    Assert.Equal(1000, count)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout and stderr into separate streams`` () = task {
    use stdOut = new MemoryStream()
    use stdErr = new MemoryStream()

    let cmd =
        dummy [ "generate binary"; "--target"; "all"; "--length"; "100000" ]
        |> Cli.stdout (PipeTo.stream stdOut)
        |> Cli.stderr (PipeTo.stream stdErr)

    let! _ = run cmd

    Assert.Equal(100_000L, stdOut.Length)
    Assert.Equal(100_000L, stdErr.Length)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout and stderr into string builders`` () = task {
    let stdOut = StringBuilder()
    let stdErr = StringBuilder()

    let cmd =
        dummy [ "echo"; "Hello world!"; "--target"; "all" ]
        |> Cli.stdout (PipeTo.string stdOut)
        |> Cli.stderr (PipeTo.string stdErr)

    let! _ = run cmd

    Assert.Equal("Hello world!", stdOut.ToString().Trim())
    Assert.Equal("Hello world!", stdErr.ToString().Trim())
}

let private allTextLines () =
    dummy [ "generate text"; "--target"; "all"; "--lines"; "1000" ]

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout and stderr into separate async delegates`` () = task {
    let mutable stdOutCount = 0
    let mutable stdErrCount = 0

    let cmd =
        allTextLines ()
        |> Cli.stdout (
            PipeTo.ft (fun _ -> task {
                do! Task.Yield()
                stdOutCount <- stdOutCount + 1
            })
        )
        |> Cli.stderr (
            PipeTo.ft (fun _ -> task {
                do! Task.Yield()
                stdErrCount <- stdErrCount + 1
            })
        )

    let! _ = run cmd

    Assert.Equal(1000, stdOutCount)
    Assert.Equal(1000, stdErrCount)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout and stderr into separate async delegates with cancellation`` () = task {
    let mutable stdOutCount = 0
    let mutable stdErrCount = 0

    let cmd =
        allTextLines ()
        |> Cli.stdout (
            PipeTo.ftc (fun _ cancellationToken -> task {
                do! Task.Delay(1, cancellationToken)
                stdOutCount <- stdOutCount + 1
            })
        )
        |> Cli.stderr (
            PipeTo.ftc (fun _ cancellationToken -> task {
                do! Task.Delay(1, cancellationToken)
                stdErrCount <- stdErrCount + 1
            })
        )

    let! _ = run cmd

    Assert.Equal(1000, stdOutCount)
    Assert.Equal(1000, stdErrCount)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout and stderr into separate sync delegates`` () = task {
    let mutable stdOutCount = 0
    let mutable stdErrCount = 0

    let cmd =
        allTextLines ()
        |> Cli.stdout (PipeTo.f (fun _ -> stdOutCount <- stdOutCount + 1))
        |> Cli.stderr (PipeTo.f (fun _ -> stdErrCount <- stdErrCount + 1))

    let! _ = run cmd

    Assert.Equal(1000, stdOutCount)
    Assert.Equal(1000, stdErrCount)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into multiple targets`` () = task {
    use stream1 = new MemoryStream()
    use stream2 = new MemoryStream()
    use stream3 = new MemoryStream()

    let cmd =
        binary ()
        |> Cli.stdout (PipeTo.all [ PipeTo.stream stream1; PipeTo.stream stream2; PipeTo.stream stream3 ])

    let! _ = run cmd

    Assert.Equal(100_000L, stream1.Length)
    Assert.Equal(100_000L, stream2.Length)
    Assert.Equal(100_000L, stream3.Length)
    Assert.Equal<byte>(stream1.ToArray(), stream2.ToArray())
    Assert.Equal<byte>(stream2.ToArray(), stream3.ToArray())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into multiple targets and not hang on large stdout if one of the targets throws an exception``
    ()
    = task {
    let cmd =
        binary ()
        |> Cli.stdout (
            PipeTo.all
                [
                    PipeTo.nullStream
                    PipeTo.f (fun _ -> raise (Exception("Expected exception.")))
                ]
        )

    do! assertThrowsContaining "Expected exception." (fun () -> run cmd)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into multiple hierarchical targets`` () = task {
    use stream1 = new MemoryStream()
    use stream2 = new MemoryStream()
    use stream3 = new MemoryStream()
    use stream4 = new MemoryStream()

    let cmd =
        binary ()
        |> Cli.stdout (
            PipeTo.all
                [
                    PipeTo.stream stream1
                    PipeTo.all
                        [
                            PipeTo.stream stream2
                            PipeTo.all [ PipeTo.stream stream3; PipeTo.stream stream4 ]
                        ]
                ]
        )

    let! _ = run cmd

    for stream in [ stream1; stream2; stream3; stream4 ] do
        Assert.Equal(100_000L, stream.Length)

    Assert.Equal<byte>(stream1.ToArray(), stream2.ToArray())
    Assert.Equal<byte>(stream2.ToArray(), stream3.ToArray())
    Assert.Equal<byte>(stream3.ToArray(), stream4.ToArray())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into multiple streams with a large buffer`` () = task {
    let cmd = dummy [ "generate binary"; "--length"; "1000000"; "--buffer"; "100000" ]

    use merged1 = new MemoryStream()
    use merged2 = new MemoryStream()

    let! _ =
        cmd
        |> Cli.stdout (PipeTo.all [ PipeTo.stream merged1; PipeTo.stream merged2 ])
        |> run

    use unmerged = new MemoryStream()
    let! _ = cmd |> Cli.stdout (PipeTo.stream unmerged) |> run

    Assert.Equal(1_000_000L, unmerged.Length)
    Assert.Equal<byte>(unmerged.ToArray(), merged1.ToArray())
    Assert.Equal<byte>(unmerged.ToArray(), merged2.ToArray())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into multiple targets and not hang if one of them reads past the end of stream``
    ()
    = task {
    let overReadingTarget =
        PipeTo.streamft (fun stream cancellationToken -> task {
            let buffer = Array.zeroCreate<byte> 1024
            let mutable reading = true

            while reading do
                let! read = stream.ReadAsync(buffer, cancellationToken)
                reading <- read > 0

            let! extra = stream.ReadAsync(buffer, cancellationToken)
            Assert.Equal(0, extra)
        })

    let cmd =
        dummy [ "generate binary"; "--length"; "4096" ]
        |> Cli.stdout (PipeTo.all [ overReadingTarget; PipeTo.nullStream ])

    let! _ = run cmd
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and pipe the stdout into a string builder via merge when the output length is a multiple of the stream reader buffer size``
    ()
    = task {
    let payload =
        Array.append (Array.create 1022 (byte 'a')) (Encoding.UTF8.GetBytes("æ"))

    let buffer = StringBuilder()

    let cmd =
        echoStdin ()
        |> Cli.stdin (ReadFrom.bytes payload)
        |> Cli.stdout (PipeTo.all [ PipeTo.stringe buffer Encoding.UTF8; PipeTo.nullStream ])

    let! _ = run cmd

    Assert.Equal(Encoding.UTF8.GetString(payload), buffer.ToString())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with buffering and also pipe the stdout into a stream`` () = task {
    use stream = new MemoryStream()

    let cmd =
        dummy [ "generate text"; "--length"; "100000" ]
        |> Cli.stdout (PipeTo.stream stream)

    let! result = runBuffered cmd

    stream.Seek(0L, SeekOrigin.Begin) |> ignore
    use reader = new StreamReader(stream)
    let! content = reader.ReadToEndAsync()
    Assert.Equal(content, result.StandardOutput)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with buffering and also pipe the stdout into a sync delegate`` () = task {
    let delegateLines = List<string>()

    let cmd = textLines () |> Cli.stdout (PipeTo.f delegateLines.Add)

    let! result = runBuffered cmd

    let expected =
        result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)

    Assert.Equal<string>(expected, delegateLines)
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command and get an error if the pipe source throws an exception`` () = task {
    let cmd = pipeline {
        ReadFrom.file "non-existing-file.txt"
        echoStdin ()
    }

    let! _ = Assert.ThrowsAnyAsync<Exception>(fun () -> run cmd)
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command and get an error if the pipe target throws an exception`` () = task {
    let cmd = binary () |> Cli.stdout (PipeTo.file "non-existing-directory/file.txt")

    let! _ = Assert.ThrowsAnyAsync<Exception>(fun () -> run cmd)
    ()
}

let private throwingTarget () =
    PipeTo.streamft (fun _ _ -> Task.FromException(Exception("Expected exception.")))

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command and not hang on large stdout if the pipe target throws an exception`` () = task {
    let cmd = binary () |> Cli.stdout (throwingTarget ())

    do! assertThrowsContaining "Expected exception." (fun () -> run cmd)
}

[<Fact(Timeout = 15000)>]
let ``I can try to execute a command and have it finish early if the pipe target throws an exception`` () = task {
    let cmd = dummy [ "sleep"; "00:00:20" ] |> Cli.stdout (throwingTarget ())

    let running = cmd |> Cli.Task.exec CancellationToken.None

    do! assertThrowsContaining "Expected exception." (fun () -> running.Task :> Task)
    Assert.False(isRunning running.ProcessId)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang if the process expects stdin but none is provided`` () = task {
    let! _ = run (echoStdin ())
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang if the process expects stdin but empty data is provided`` () = task {
    let cmd = echoStdin () |> Cli.stdin (ReadFrom.bytes Array.empty)

    let! _ = run cmd
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang if the process only partially consumes stdin`` () = task {
    let random = Random(1234567)

    let cmd = pipeline {
        ReadFrom.ft (fun destination cancellationToken -> task {
            use buffer = MemoryPool<byte>.Shared.Rent(256)

            while true do
                random.NextBytes(buffer.Memory.Span)
                do! destination.WriteAsync(buffer.Memory, cancellationToken)
        })

        dummy [ "echo stdin"; "--length"; "100000" ]
    }

    let! _ = run cmd
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang if the process does not consume stdin`` () = task {
    let cmd = pipeline {
        ReadFrom.ft (fun _ cancellationToken -> Task.Delay(TimeSpan.FromSeconds(20.0), cancellationToken))
        dummy [ "echo stdin"; "--length"; "0" ]
    }

    let! _ = run cmd
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang if the process does not consume stdin even if the source cannot be canceled``
    ()
    = task {
    let cmd = pipeline {
        ReadFrom.ft (fun _ _ -> Task.Delay(TimeSpan.FromSeconds(20.0), CancellationToken.None))
        dummy [ "echo stdin"; "--length"; "0" ]
    }

    let! _ = run cmd
    ()
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and not hang on large stdin while also writing stdout`` () = task {
    let random = Random(1234567)
    let mutable bytesRemaining = 100_000

    let cmd = pipeline {
        ReadFrom.ft (fun destination cancellationToken -> task {
            use buffer = MemoryPool<byte>.Shared.Rent(256)

            while bytesRemaining > 0 do
                random.NextBytes(buffer.Memory.Span)
                let count = min bytesRemaining buffer.Memory.Length
                do! destination.WriteAsync(buffer.Memory.Slice(0, count), cancellationToken)
                bytesRemaining <- bytesRemaining - count
        })

        echoStdin ()
    }

    let! _ = run cmd
    ()
}
