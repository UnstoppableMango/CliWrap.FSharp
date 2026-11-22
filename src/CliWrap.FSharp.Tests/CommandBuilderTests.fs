module CommandBuilderTests

open System.Collections.Generic
open System.Text
open System.Threading
open CliWrap
open CliWrap.Buffered
open CliWrap.Tests
open Xunit
open FsCheck
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

[<Property>]
let ``Should configure target file path`` target =
    let expected = Command(target)
    let actual = command target { args [] }
    Equivalence.assertEquivalent expected actual

[<Property>]
let ``Should configure args`` (a: NonNull<string> list) =
    let input = a |> List.map _.Get
    let expected = Command(Dummy.Program.FilePath).WithArguments(input)
    let actual = command Dummy.Program.FilePath { args input }
    Equivalence.assertEquivalent expected actual

[<Property>]
let ``Should configure string args`` (a: NonNull<string>) =
    let expected = Command(Dummy.Program.FilePath).WithArguments(a.Get)
    let actual = command Dummy.Program.FilePath { args a.Get }
    Equivalence.assertEquivalent expected actual

// Each run asks the Dummy program to exit with a non-zero code, so a passing test shows
// the builder ran the command it configured rather than some default.
let private exitCode = 3

[<Fact>]
let ``Should execute CommandTask asynchronously`` () = task {
    let! actual =
        command Dummy.Program.FilePath {
            args [ "exit"; string exitCode ]
            validation CommandResultValidation.None
            exec
            async
        }
        |> Async.StartAsTask

    Assert.Equal(exitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously`` () = task {
    let! actual =
        command Dummy.Program.FilePath {
            args [ "exit"; string exitCode ]
            validation CommandResultValidation.None
            async
        }
        |> Async.StartAsTask

    Assert.Equal(exitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with cancellation`` () = task {
    use cts = new CancellationTokenSource()

    let! actual =
        command Dummy.Program.FilePath {
            args [ "exit"; string exitCode ]
            validation CommandResultValidation.None
            async cts.Token
        }
        |> Async.StartAsTask

    Assert.Equal(exitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously as task`` () = task {
    let! actual = command Dummy.Program.FilePath {
        args [ "exit"; string exitCode ]
        validation CommandResultValidation.None
        exec
    }

    Assert.Equal(exitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute buffered`` () = task {
    let! actual = command Dummy.Program.FilePath {
        args [ "echo"; "hello" ]
        buffered
    }

    Assert.Equal("hello", actual.StandardOutput.Trim())
}

[<Fact>]
let ``Should execute buffered with encoding`` () = task {
    let! actual = command Dummy.Program.FilePath {
        args [ "echo"; "hello" ]
        buffered Encoding.UTF8
    }

    Assert.Equal("hello", actual.StandardOutput.Trim())
}

[<Fact>]
let ``Should execute buffered with encoding and cancellation`` () = task {
    use cts = new CancellationTokenSource()

    let! actual = command Dummy.Program.FilePath {
        args [ "echo"; "hello" ]
        buffered Encoding.UTF8 cts.Token
    }

    Assert.Equal("hello", actual.StandardOutput.Trim())
}

[<Property>]
let ``Should configure environment variables`` var =
    let expected =
        Command(Dummy.Program.FilePath).WithEnvironmentVariables((dict [ var ]).AsReadOnly())

    let actual = command Dummy.Program.FilePath { env [ var ] }
    Equivalence.assertEquivalent expected actual

[<Fact>]
let ``Should configure stdin`` () =
    let input = PipeSource.FromString "testing"
    let expected = Command(Dummy.Program.FilePath).WithStandardInputPipe(input)

    let actual = command Dummy.Program.FilePath { stdin input }

    Equivalence.assertEquivalent expected actual

[<Fact>]
let ``Should configure stdout`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let expected = Command(Dummy.Program.FilePath).WithStandardOutputPipe(pipe)

    let actual = command Dummy.Program.FilePath { stdout pipe }

    Equivalence.assertEquivalent expected actual

[<Fact>]
let ``Should configure stderr`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let expected = Command(Dummy.Program.FilePath).WithStandardErrorPipe(pipe)

    let actual = command Dummy.Program.FilePath { stderr pipe }

    Equivalence.assertEquivalent expected actual

[<Fact>]
let ``Should configure validation`` () =
    let expected =
        Command(Dummy.Program.FilePath).WithValidation(CommandResultValidation.None)

    let actual = command Dummy.Program.FilePath { validation CommandResultValidation.None }

    Equivalence.assertEquivalent expected actual

[<Property>]
let ``Should configure working directory`` directory =
    let expected = Command(Dummy.Program.FilePath).WithWorkingDirectory(directory)
    let actual = command Dummy.Program.FilePath { workingDirectory directory }
    Equivalence.assertEquivalent expected actual
