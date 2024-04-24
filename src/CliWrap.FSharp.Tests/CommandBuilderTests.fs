module CommandBuilderTests

open System.Collections.Generic
open System.IO
open System.Linq
open System.Text
open System.Threading
open CliWrap
open CliWrap.Buffered
open Xunit
open FsCheck
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

[<Property>]
let ``Should configure target file path`` target =
    let expected = Command(target)
    let actual = command target { args [] }
    actual.TargetFilePath = expected.TargetFilePath

[<Property>]
let ``Should configure args`` (a: NonNull<string> list) =
    let input = a |> List.map _.Get
    let expected = Command(Tests.Dummy.Program.FilePath).WithArguments(input)
    let actual = command Tests.Dummy.Program.FilePath { args input }
    actual.Arguments = expected.Arguments

[<Property>]
let ``Should configure string args`` (a: NonNull<string>) =
    let expected = Command(Tests.Dummy.Program.FilePath).WithArguments(a.Get)
    let actual = command Tests.Dummy.Program.FilePath { args a.Get }
    actual.Arguments = expected.Arguments

[<Fact>]
let ``Should execute CommandTask asynchronously`` () = task {
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteAsync()

    let! actual =
        command Tests.Dummy.Program.FilePath {
            exec
            async
        }
        |> Async.StartAsTask

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously`` () = task {
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteAsync()
    let! actual = command Tests.Dummy.Program.FilePath { async } |> Async.StartAsTask
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with cancellation`` () = task {
    use cts = new CancellationTokenSource()
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteAsync(cts.Token)
    let! actual = command Tests.Dummy.Program.FilePath { async cts.Token } |> Async.StartAsTask
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously as task`` () = task {
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteAsync()
    let! actual = command Tests.Dummy.Program.FilePath { exec }
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute buffered`` () = task {
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteBufferedAsync()
    let! actual = command Tests.Dummy.Program.FilePath { buffered }
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute buffered with encoding`` () = task {
    let encoding = Encoding.UTF8
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteBufferedAsync(encoding)
    let! actual = command Tests.Dummy.Program.FilePath { buffered encoding }
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute buffered with encoding and cancellation`` () = task {
    let encoding = Encoding.UTF8
    use cts = new CancellationTokenSource()
    let! expected = Command(Tests.Dummy.Program.FilePath).ExecuteBufferedAsync(encoding, cts.Token)
    let! actual = command Tests.Dummy.Program.FilePath { buffered encoding cts.Token }
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Property>]
let ``Should configure environment variables`` var =
    let expected =
        Command(Tests.Dummy.Program.FilePath)
            .WithEnvironmentVariables((dict [ var ]).AsReadOnly())

    let actual = command Tests.Dummy.Program.FilePath { env [ var ] }
    actual.EnvironmentVariables.SequenceEqual(expected.EnvironmentVariables)

[<Fact>]
let ``Should configure stdin`` () = task {
    let input = PipeSource.FromString "testing"
    let expected = Command(Tests.Dummy.Program.FilePath).WithStandardInputPipe(input)

    let actual = command Tests.Dummy.Program.FilePath { stdin input }

    let a, b = new MemoryStream(), new MemoryStream()
    do! expected.StandardInputPipe.CopyToAsync(a)
    do! actual.StandardInputPipe.CopyToAsync(b)

    Assert.Equal<byte>(a.ToArray(), b.ToArray())
}

[<Fact>]
let ``Should configure stdout`` () = task {
    let a, b = StringBuilder(), StringBuilder()

    let! _ =
        Command(Tests.Dummy.Program.FilePath)
            .WithArguments([ "generate binary"; "--target"; "all" ])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(a))
            .ExecuteAsync()

    let! _ = command Tests.Dummy.Program.FilePath {
        args [ "generate binary"; "--target"; "all" ]
        stdout (PipeTarget.ToStringBuilder(b))
        exec
    }

    Assert.Equal(a.ToString(), b.ToString())
}

[<Fact>]
let ``Should configure stderr`` () = task {
    let a, b = StringBuilder(), StringBuilder()

    let! _ =
        Command(Tests.Dummy.Program.FilePath)
            .WithArguments([ "generate binary"; "--target"; "all" ])
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(a))
            .ExecuteAsync()

    let! _ = command Tests.Dummy.Program.FilePath {
        args [ "generate binary"; "--target"; "all" ]
        validation CommandResultValidation.None
        stderr (PipeTarget.ToStringBuilder(b))
        exec
    }

    Assert.Equal(a.ToString(), b.ToString())
}

[<Property>]
let ``Should configure working directory`` directory =
    let expected = Command(Tests.Dummy.Program.FilePath).WithWorkingDirectory(directory)
    let actual = command Tests.Dummy.Program.FilePath { workingDirectory directory }
    actual.WorkingDirPath = expected.WorkingDirPath
