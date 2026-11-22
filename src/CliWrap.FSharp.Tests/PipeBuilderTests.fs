module PipeBuilderTests

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks
open CliWrap
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

let private dummy (args: string list) =
    Cli.wrap Dummy.Program.FilePath |> Cli.args args

let private echoStdin () = dummy [ "echo stdin" ]

/// Writes "hello" to stdout only, so a target that gets stderr stays empty.
let private echoStdout () =
    dummy [ "echo"; "hello"; "--target"; "stdout" ]

let private runBuffered (cmd: Command) =
    cmd |> Cli.execBuffered |> Async.StartAsTask

let private run (cmd: Command) = cmd |> Cli.exec |> Async.StartAsTask

let private readUtf8 (stream: MemoryStream) =
    Encoding.UTF8.GetString(stream.ToArray()).Trim()

[<Fact(Timeout = 15000)>]
let ``Should run every stage of a three command chain in order`` () = task {
    let cmd = pipeline {
        dummy [ "generate binary"; "--length"; "100000" ]
        dummy [ "echo stdin"; "--length"; "5" ]
        dummy [ "length stdin" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("5", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should feed a leading source into the first command of a chain`` () = task {
    let cmd = pipeline {
        "Hello world"
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command into a lone PipeTarget`` () = task {
    let stdout = StringBuilder()

    let cmd = pipeline {
        echoStdout ()
        PipeTarget.ToStringBuilder(stdout)
    }

    let! _ = run cmd

    Assert.Equal("hello", stdout.ToString().Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command into a lone Stream`` () = task {
    use stdout = new MemoryStream()
    let target: Stream = stdout

    let cmd = pipeline {
        echoStdout ()
        target
    }

    let! _ = run cmd

    Assert.Equal("hello", readUtf8 stdout)
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command into a lone StringBuilder`` () = task {
    let stdout = StringBuilder()

    let cmd = pipeline {
        echoStdout ()
        stdout
    }

    let! _ = run cmd

    Assert.Equal("hello", stdout.ToString().Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command into a lone delegate`` () = task {
    let lines = List<string>()
    let target (line: string) = lines.Add(line)

    let cmd = pipeline {
        echoStdout ()
        target
    }

    let! _ = run cmd

    Assert.Equal<string>([ "hello" ], lines)
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command into a lone async delegate`` () = task {
    let lines = List<string>()

    let target (line: string) (_: CancellationToken) =
        lines.Add(line)
        Task.CompletedTask

    let cmd = pipeline {
        echoStdout ()
        target
    }

    let! _ = run cmd

    Assert.Equal<string>([ "hello" ], lines)
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout of the last command of a chain into a lone target`` () = task {
    let stdout = StringBuilder()

    let cmd = pipeline {
        "Hello world"
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
        stdout
    }

    let! _ = run cmd

    Assert.Equal("Hello", stdout.ToString().Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout and stderr into a trailing pair of targets`` () = task {
    let stdout = StringBuilder()
    let stderr = StringBuilder()

    let cmd = pipeline {
        dummy [ "echo"; "hello"; "--target"; "all" ]
        PipeTarget.ToStringBuilder(stdout), PipeTarget.ToStringBuilder(stderr)
    }

    let! _ = run cmd

    Assert.Equal("hello", stdout.ToString().Trim())
    Assert.Equal("hello", stderr.ToString().Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should keep stdout first and stderr second in a trailing pair of targets`` () = task {
    let stdout = StringBuilder()
    let stderr = StringBuilder()

    let cmd = pipeline {
        dummy [ "echo"; "hello"; "--target"; "stderr" ]
        stdout, stderr
    }

    let! _ = run cmd

    Assert.Equal("", stdout.ToString().Trim())
    Assert.Equal("hello", stderr.ToString().Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should pipe stdout and stderr into a trailing pair of streams`` () = task {
    use stdout = new MemoryStream()
    use stderr = new MemoryStream()
    let targets: Stream * Stream = stdout, stderr

    let cmd = pipeline {
        dummy [ "echo"; "hello"; "--target"; "stdout" ]
        targets
    }

    let! _ = run cmd

    Assert.Equal("hello", readUtf8 stdout)
    Assert.Equal("", readUtf8 stderr)
}

[<Fact(Timeout = 15000)>]
let ``Should feed a leading PipeSource into a chain`` () = task {
    let cmd = pipeline {
        PipeSource.FromString("Hello world")
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should accept a byte array as the first item`` () = task {
    let cmd = pipeline {
        "Hello world"B
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should accept read-only memory as the first item`` () = task {
    let cmd = pipeline {
        ReadOnlyMemory("Hello world"B)
        echoStdin ()
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello world", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should feed a leading byte array into a chain`` () = task {
    let cmd = pipeline {
        "Hello world"B
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should feed leading read-only memory into a chain`` () = task {
    let cmd = pipeline {
        ReadOnlyMemory("Hello world"B)
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``Should feed a leading Stream into a chain`` () = task {
    use source = new MemoryStream("Hello world"B)
    let source: Stream = source

    let cmd = pipeline {
        source
        echoStdin ()
        dummy [ "echo stdin"; "--length"; "5" ]
    }

    let! result = runBuffered cmd

    Assert.Equal("Hello", result.StandardOutput.Trim())
}

[<Fact>]
let ``Should return the command itself for a single command`` () =
    let expected = dummy [ "echo"; "hello" ]
    let actual = pipeline { expected }
    Assert.Same(expected, actual)
