/// Conformance specs ported from upstream's CliWrap.Tests/LineBreakSpecs.cs.
/// Upstream pipes with `data | cmd | lines.Add`. The `pipeline` builder can't yield a
/// delegate target, so stdout goes through `Cli.stdout` instead.
module Conformance.LineBreakSpecs

open System.Collections.Generic
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp

let private echoLines (data: string) = task {
    let lines = List<string>()

    let cmd =
        pipeline {
            data
            Cli.wrap Dummy.Program.FilePath |> Cli.arg "echo stdin"
        }
        |> Cli.stdout (PipeTo.f lines.Add)

    let! _ = cmd |> Cli.exec
    return List.ofSeq lines
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and split the stdout by newline`` () = task {
    let! lines = echoLines "Foo\nBar\nBaz"
    Assert.Equal<string list>([ "Foo"; "Bar"; "Baz" ], lines)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and split the stdout by caret return`` () = task {
    let! lines = echoLines "Foo\rBar\rBaz"
    Assert.Equal<string list>([ "Foo"; "Bar"; "Baz" ], lines)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and split the stdout by caret return followed by newline`` () = task {
    let! lines = echoLines "Foo\r\nBar\r\nBaz"
    Assert.Equal<string list>([ "Foo"; "Bar"; "Baz" ], lines)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command and split the stdout by newline while including empty lines`` () = task {
    let! lines = echoLines "Foo\r\rBar\n\nBaz"
    Assert.Equal<string list>([ "Foo"; ""; "Bar"; ""; "Baz" ], lines)
}
