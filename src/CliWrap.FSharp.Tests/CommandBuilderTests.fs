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
    let expected = Command("").WithArguments(input)
    let result = command "/dev/null" { args input }
    result.Arguments = expected.Arguments

[<Property>]
let ``Should configure string args`` (a: NonNull<string>) =
    let expected = Command("").WithArguments(a.Get)
    let result = command "/dev/null" { args a.Get }
    result.Arguments = expected.Arguments

[<Property>]
let ``Should configure working directory`` directory =
    let expected = Command("").WithWorkingDirectory(directory)
    let result = command "/dev/null" { workingDirectory directory }
    result.WorkingDirPath = expected.WorkingDirPath

[<Property>]
let ``Should configure environment variables`` var =
    let expected = Command("").WithEnvironmentVariables((dict [ var ]).AsReadOnly())
    let result = command "/dev/null" { env [ var ] }
    result.EnvironmentVariables.SequenceEqual(expected.EnvironmentVariables)

[<Property>]
let ``Should configure stdin`` (input: NonNull<string>) =
    let expected = Command("").WithStandardInputPipe(input.Get |> PipeSource.FromString)

    let result = command "/dev/null" { stdin (input.Get |> PipeSource.FromString) }
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
            .WithValidation(CommandResultValidation.None)
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(a))

    let result = command "echo" {
        args [ "testing" ]
        stderr (PipeTarget.ToStringBuilder(b))
    }

    expected.ExecuteAsync().Task.Wait()
    result.WithValidation(CommandResultValidation.None).ExecuteAsync().Task.Wait()

    a.ToString() = b.ToString()
