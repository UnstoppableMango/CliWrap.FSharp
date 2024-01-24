module CliTests

open System.Collections.Generic
open System.Linq
open CliWrap
open CliWrap.Tests
open Xunit
open FsCheck
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

[<Property>]
let ``Should create command`` target =
    let expected = Command(target)
    let actual = Cli.wrap target
    expected.TargetFilePath = actual.TargetFilePath

[<Property>]
let ``Should configure a single argument`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithArguments(arg.Get)
    let actual = cmd |> Cli.arg arg.Get
    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithArguments([ arg.Get ])
    let actual = cmd |> Cli.args [ arg.Get ]
    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments with escape`` (arg: NonNull<string>) escape =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithArguments([ arg.Get ], escape)
    let actual = cmd |> Cli.argse [ arg.Get ] escape
    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments with builder`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithArguments(fun b -> b.Add(arg.Get) |> ignore)
    let actual = cmd |> Cli.argsf _.Add(arg.Get)
    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure credentials`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithCredentials(Credentials(arg.Get))
    let actual = cmd |> Cli.creds (Credentials(arg.Get))
    expected.Credentials.Domain = actual.Credentials.Domain

[<Property>]
let ``Should configure credentials with builder`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithCredentials(fun b -> b.SetUserName(arg.Get) |> ignore)
    let actual = cmd |> Cli.credsf _.SetUserName(arg.Get)
    expected.Credentials.UserName = actual.Credentials.UserName

[<Property>]
let ``Should configure environment variables`` (key: NonNull<string>) (value: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)

    let expected =
        cmd.WithEnvironmentVariables((dict [ key.Get, value.Get ]).AsReadOnly())

    let actual = cmd |> Cli.env [ key.Get, value.Get ]
    expected.EnvironmentVariables.SequenceEqual(actual.EnvironmentVariables)

[<Property>]
let ``Should configure environment variables with builder`` (key: NonNull<string>) (value: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)

    let expected =
        cmd.WithEnvironmentVariables(fun b -> b.Set(key.Get, value.Get) |> ignore)

    let actual = cmd |> Cli.envf _.Set(key.Get, value.Get)
    expected.EnvironmentVariables.SequenceEqual(actual.EnvironmentVariables)

[<Fact>]
let ``Should execute asynchronously`` = async {
    let cmd = Command(Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync() |> CommandTask.op_Implicit |> Async.AwaitTask
    let! actual = cmd |> Cli.exec
    Assert.Equal(expected.ExitCode, actual.ExitCode)
}
