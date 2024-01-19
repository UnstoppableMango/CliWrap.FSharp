module CommandBuilderTests

open System.Collections.Generic
open System.IO
open System.Linq
open System.Text
open CliWrap
open CliWrap.Tests
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
    let expected = Command(Dummy.Program.FilePath).WithArguments(input)
    let actual = command Dummy.Program.FilePath { args input }
    actual.Arguments = expected.Arguments

[<Property>]
let ``Should configure string args`` (a: NonNull<string>) =
    let expected = Command(Dummy.Program.FilePath).WithArguments(a.Get)
    let actual = command Dummy.Program.FilePath { args a.Get }
    actual.Arguments = expected.Arguments

[<Property>]
let ``Should execute CommandTask asynchronously`` () =
    let expected = Command(Dummy.Program.FilePath).ExecuteAsync().Task.Result

    let actual =
        command Dummy.Program.FilePath {
            exec
            async
        }
        |> Async.RunSynchronously

    actual.ExitCode = expected.ExitCode

[<Property>]
let ``Should execute asynchronously`` () =
    let expected = Command(Dummy.Program.FilePath).ExecuteAsync().Task.Result
    let actual = command Dummy.Program.FilePath { async } |> Async.RunSynchronously
    actual.ExitCode = expected.ExitCode

[<Property>]
let ``Should configure environment variables`` var =
    let expected =
        Command(Dummy.Program.FilePath)
            .WithEnvironmentVariables((dict [ var ]).AsReadOnly())

    let actual = command Dummy.Program.FilePath { env [ var ] }
    actual.EnvironmentVariables.SequenceEqual(expected.EnvironmentVariables)

[<Property>]
let ``Should configure stdin`` (input: NonNull<string>) =
    let expected =
        Command(Dummy.Program.FilePath)
            .WithStandardInputPipe(input.Get |> PipeSource.FromString)

    let actual = command Dummy.Program.FilePath { stdin (input.Get |> PipeSource.FromString) }
    let a, b = new MemoryStream(), new MemoryStream()
    expected.StandardInputPipe.CopyToAsync(a).Wait()
    actual.StandardInputPipe.CopyToAsync(b).Wait()

    a.ToArray() = b.ToArray()

[<Property>]
let ``Should configure stdout`` () =
    let a, b = StringBuilder(), StringBuilder()

    let expected =
        Command(Dummy.Program.FilePath)
            .WithArguments([ "generate binary"; "--target"; "all" ])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(a))

    let actual = command Dummy.Program.FilePath {
        args [ "generate binary"; "--target"; "all" ]
        stdout (PipeTarget.ToStringBuilder(b))
    }

    expected.ExecuteAsync().Task.Wait()
    actual.ExecuteAsync().Task.Wait()
    a.ToString() = b.ToString()

[<Property>]
let ``Should configure stderr`` () =
    let a, b = StringBuilder(), StringBuilder()

    let expected =
        Command(Dummy.Program.FilePath)
            .WithArguments([ "generate binary"; "--target"; "all" ])
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(a))

    let actual = command Dummy.Program.FilePath {
        args [ "generate binary"; "--target"; "all" ]
        validation CommandResultValidation.None
        stderr (PipeTarget.ToStringBuilder(b))
    }

    expected.ExecuteAsync().Task.Wait()
    actual.ExecuteAsync().Task.Wait()

    a.ToString() = b.ToString()

[<Property>]
let ``Should configure working directory`` directory =
    let expected = Command(Dummy.Program.FilePath).WithWorkingDirectory(directory)
    let actual = command Dummy.Program.FilePath { workingDirectory directory }
    actual.WorkingDirPath = expected.WorkingDirPath
