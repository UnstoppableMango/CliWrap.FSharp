module CommandBuilderTests

open System.Collections.Generic
open System.IO
open System.Linq
open System.Text
open CliWrap
open FsCheck
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

[<Property>]
let ``Should configure target file path`` target =
    let expected = Command(target)
    let result = command target { args [] }
    result.TargetFilePath = expected.TargetFilePath

[<Property>]
let ``Should configure args`` (a: NonNull<string> list) =
    let input = a |> List.map _.Get
    let expected = Command("expected").WithArguments(input)
    let result = command "actual" { args input }
    result.Arguments = expected.Arguments

[<Property>]
let ``Should configure string args`` (a: NonNull<string>) =
    let expected = Command("expected").WithArguments(a.Get)
    let result = command "actual" { args a.Get }
    result.Arguments = expected.Arguments

[<Property>]
let ``Should execute CommandTask asynchronously`` () =
    let expected =
        Command("echo").WithArguments([ "testing" ]).ExecuteAsync().Task.Result

    let result =
        command "actual" {
            args [ "testing" ]
            exec
            async
        }
        |> Async.RunSynchronously

    // TODO: Better assertion. Can this be tested w/o using `stdout`?
    result.ExitCode = expected.ExitCode

[<Property>]
let ``Should execute asynchronously`` () =
    let expected =
        Command("echo").WithArguments([ "testing" ]).ExecuteAsync().Task.Result

    let result =
        command "actual" {
            args [ "testing" ]
            async
        }
        |> Async.RunSynchronously

    // TODO: Better assertion. Can this be tested w/o using `stdout`?
    result.ExitCode = expected.ExitCode

[<Property>]
let ``Should configure environment variables`` var =
    let expected =
        Command("expected").WithEnvironmentVariables((dict [ var ]).AsReadOnly())

    let result = command "actual" { env [ var ] }
    result.EnvironmentVariables.SequenceEqual(expected.EnvironmentVariables)

[<Property>]
let ``Should configure stdin`` (input: NonNull<string>) =
    let expected =
        Command("expected").WithStandardInputPipe(input.Get |> PipeSource.FromString)

    let result = command "actual" { stdin (input.Get |> PipeSource.FromString) }
    let a, b = new MemoryStream(), new MemoryStream()
    expected.StandardInputPipe.CopyToAsync(a).Wait()
    result.StandardInputPipe.CopyToAsync(b).Wait()

    a.ToArray() = b.ToArray()

[<Property>]
let ``Should configure stdout`` () =
    let a, b = StringBuilder(), StringBuilder()

    let expected =
        Command("echo")
            .WithArguments([ "testing" ])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(a))

    let result = command "echo" {
        args [ "testing" ]
        stdout (PipeTarget.ToStringBuilder(b))
    }

    expected.ExecuteAsync().Task.Wait()
    result.ExecuteAsync().Task.Wait()
    a.ToString() = b.ToString()

[<Property>]
let ``Should configure stderr`` () =
    let a, b = StringBuilder(), StringBuilder()

    let expected =
        Command("echo")
            .WithArguments([ "testing" ])
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(a))

    let result = command "echo" {
        args [ "testing" ]
        stderr (PipeTarget.ToStringBuilder(b))
    }

    expected.ExecuteAsync().Task.Wait()
    result.ExecuteAsync().Task.Wait()

    a.ToString() = b.ToString()

[<Property>]
let ``Should configure working directory`` directory =
    let expected = Command("expected").WithWorkingDirectory(directory)
    let result = command "actual" { workingDirectory directory }
    result.WorkingDirPath = expected.WorkingDirPath
