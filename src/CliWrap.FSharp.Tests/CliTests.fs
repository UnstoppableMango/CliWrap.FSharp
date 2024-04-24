module CliTests

open System.Collections.Generic
open System.Linq
open System.Text
open System.Threading
open CliWrap
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
let ``Should create command the long way``
    target
    (args: NonNull<string>)
    (workDir: NonNull<string>)
    (domain: NonNull<string>)
    (stdin: NonNull<string>)
    =
    let input = PipeSource.FromString stdin.Get
    let output = PipeTarget.ToStringBuilder(StringBuilder())
    let err = PipeTarget.ToStringBuilder(StringBuilder())
    let creds = Credentials(domain.Get)
    let envs = (dict [ "test", "test" ]).AsReadOnly()
    let validation = CommandResultValidation.None
    let expected = Command(target)

    let actual =
        Cli.commandv target args.Get workDir.Get creds envs validation input output err

    expected.TargetFilePath = actual.TargetFilePath

[<Property>]
let ``Should configure a single argument`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithArguments(arg.Get)

    let actual = cmd |> Cli.arg arg.Get

    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithArguments([ arg.Get ])

    let actual = cmd |> Cli.args [ arg.Get ]

    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments with escape`` (arg: NonNull<string>) escape =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithArguments([ arg.Get ], escape)

    let actual = cmd |> Cli.argse [ arg.Get ] escape

    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure arguments with builder`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithArguments(fun b -> b.Add(arg.Get) |> ignore)

    let actual = cmd |> Cli.argsf _.Add(arg.Get)

    expected.Arguments = actual.Arguments

[<Property>]
let ``Should configure credentials`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithCredentials(Credentials(arg.Get))

    let actual = cmd |> Cli.creds (Credentials(arg.Get))

    expected.Credentials.Domain = actual.Credentials.Domain

[<Property>]
let ``Should configure credentials with builder`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithCredentials(fun b -> b.SetUserName(arg.Get) |> ignore)

    let actual = cmd |> Cli.credsf _.SetUserName(arg.Get)

    expected.Credentials.UserName = actual.Credentials.UserName

[<Property>]
let ``Should configure environment variables`` (key: NonNull<string>) (value: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)

    let expected =
        cmd.WithEnvironmentVariables((dict [ key.Get, value.Get ]).AsReadOnly())

    let actual = cmd |> Cli.env [ key.Get, value.Get ]
    expected.EnvironmentVariables.SequenceEqual(actual.EnvironmentVariables)

[<Property>]
let ``Should configure environment variables with builder`` (key: NonNull<string>) (value: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)

    let expected =
        cmd.WithEnvironmentVariables(fun b -> b.Set(key.Get, value.Get) |> ignore)

    let actual = cmd |> Cli.envf _.Set(key.Get, value.Get)
    expected.EnvironmentVariables.SequenceEqual(actual.EnvironmentVariables)

[<Fact>]
let ``Should execute asynchronously`` () = task {
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync()

    let! actual = cmd |> Cli.exec

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with cancellation`` () = task {
    use cts = new CancellationTokenSource()
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync(cts.Token)

    let! actual = cmd |> Cli.Task.exec cts.Token

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with forceful and graceful tokens`` () = task {
    use forceful = new CancellationTokenSource()
    use graceful = new CancellationTokenSource()
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync(forceful.Token, graceful.Token)

    let! actual = cmd |> Cli.Task.execf forceful.Token graceful.Token

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Property>]
let ``Should configure stdin`` (value: NonNull<string>) =
    let input = PipeSource.FromString value.Get
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithStandardInputPipe(input)

    let actual = cmd |> Cli.stdin input

    expected.StandardInputPipe = actual.StandardInputPipe

[<Property>]
let ``Should configure stdout`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithStandardOutputPipe(pipe)

    let actual = cmd |> Cli.stdout pipe

    expected.StandardOutputPipe = actual.StandardOutputPipe

[<Property>]
let ``Should configure stderr`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithStandardErrorPipe(pipe)

    let actual = cmd |> Cli.stderr pipe

    expected.StandardOutputPipe = actual.StandardOutputPipe

[<Property>]
let ``Should configure target file`` (file: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithTargetFile(file.Get)

    let actual = cmd |> Cli.target file.Get

    expected.TargetFilePath = actual.TargetFilePath

[<Property>]
let ``Should configure validation`` () =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithValidation(CommandResultValidation.None)

    let actual = cmd |> Cli.validation CommandResultValidation.None

    expected.Validation = actual.Validation

[<Property>]
let ``Should configure working directory`` (dir: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath)
    let expected = cmd.WithWorkingDirectory(dir.Get)

    let actual = cmd |> Cli.workDir dir.Get

    expected.WorkingDirPath = actual.WorkingDirPath

[<Property>]
let ``Should convert to string`` (arg: NonNull<string>) =
    let cmd = Command(Tests.Dummy.Program.FilePath).WithArguments([ arg.Get ])
    let expected = cmd.ToString()

    let actual = cmd |> Cli.toString

    expected = actual
