module CliTests

open System
open System.Collections.Generic
open System.Diagnostics
open System.Linq
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
    let policy = ResourcePolicy()
    let creds = Credentials(domain.Get)
    let envs = (dict [ "test", "test" ]).AsReadOnly()
    let validation = CommandResultValidation.None
    let expected = Command(target)

    let actual =
        Cli.commandv target args.Get workDir.Get policy creds envs validation input output err

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

[<Fact>]
let ``Should configure resource policy`` () =
    let policy = ResourcePolicy(ProcessPriorityClass.BelowNormal)
    let cmd = Command(Dummy.Program.FilePath)

    let actual = cmd |> Cli.resourcePolicy policy

    Assert.Same(policy, actual.ResourcePolicy)

[<Fact>]
let ``Should configure resource policy with builder`` () =
    let cmd = Command(Dummy.Program.FilePath)

    let actual =
        cmd |> Cli.resourcePolicyf _.SetPriority(ProcessPriorityClass.BelowNormal)

    Assert.Equal(Nullable ProcessPriorityClass.BelowNormal, actual.ResourcePolicy.Priority)

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
let ``Should execute asynchronously`` () = task {
    let cmd = Command(Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync()

    let! actual = cmd |> Cli.exec

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with cancellation`` () = task {
    use cts = new CancellationTokenSource()
    let cmd = Command(Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync(cts.Token)

    let! actual = cmd |> Cli.Task.exec cts.Token

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

[<Fact>]
let ``Should execute asynchronously with forceful and graceful tokens`` () = task {
    use forceful = new CancellationTokenSource()
    use graceful = new CancellationTokenSource()
    let cmd = Command(Dummy.Program.FilePath)
    let! expected = cmd.ExecuteAsync(forceful.Token, graceful.Token)

    let! actual = cmd |> Cli.Task.execf forceful.Token graceful.Token

    Assert.Equal(expected.ExitCode, actual.ExitCode)
}

let private sleepCommand (output: StringBuilder) =
    Command(Dummy.Program.FilePath)
        .WithArguments([ "sleep"; "00:00:20" ])
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))

[<Fact>]
let ``Should cancel gracefully with the graceful token`` () = task {
    use forceful = new CancellationTokenSource()
    use graceful = new CancellationTokenSource(TimeSpan.FromSeconds(1.0))
    let output = StringBuilder()

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () ->
            (sleepCommand output |> Cli.Task.execf forceful.Token graceful.Token).Task :> Tasks.Task)

    Assert.Contains("Canceled.", output.ToString())
}

[<Fact>]
let ``Should cancel forcefully with the forceful token`` () = task {
    use forceful = new CancellationTokenSource(TimeSpan.FromSeconds(1.0))
    use graceful = new CancellationTokenSource()
    let output = StringBuilder()

    let! _ =
        Assert.ThrowsAnyAsync<OperationCanceledException>(fun () ->
            (sleepCommand output |> Cli.Task.execf forceful.Token graceful.Token).Task :> Tasks.Task)

    Assert.DoesNotContain("Canceled.", output.ToString())
}

[<Fact>]
let ``Should configure the process before execution`` () = task {
    let output = StringBuilder()

    let cmd =
        Command(Dummy.Program.FilePath)
            .WithArguments([ "env"; "CLIWRAP_FSHARP_TEST" ])
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))

    let mutable configured = false

    let! _ =
        cmd
        |> Cli.Task.execp
            (fun psi -> psi.Environment["CLIWRAP_FSHARP_TEST"] <- "from-psi")
            (fun _ -> configured <- true)
            CancellationToken.None
            CancellationToken.None

    Assert.Contains("from-psi", output.ToString())
    Assert.True(configured)
}

let private echoCommand =
    Command(Dummy.Program.FilePath).WithArguments([ "echo"; "hello" ])

[<Fact>]
let ``Should execute buffered`` () = task {
    let! expected = echoCommand.ExecuteBufferedAsync()

    let! actual = echoCommand |> Cli.execBuffered

    Assert.Equal(expected.ExitCode, actual.ExitCode)
    Assert.Equal(expected.StandardOutput, actual.StandardOutput)
    Assert.Contains("hello", actual.StandardOutput)
}

[<Fact>]
let ``Should execute buffered with encoding`` () = task {
    let! actual = echoCommand |> Cli.execBufferede Encoding.UTF8

    Assert.Contains("hello", actual.StandardOutput)
}

[<Fact>]
let ``Should execute buffered with cancellation`` () = task {
    use cts = new CancellationTokenSource()

    let! actual = echoCommand |> Cli.Task.execBuffered cts.Token

    Assert.Contains("hello", actual.StandardOutput)
}

[<Fact>]
let ``Should execute buffered with encoding and cancellation`` () = task {
    use cts = new CancellationTokenSource()

    let! actual = echoCommand |> Cli.Task.execBufferede Encoding.UTF8 cts.Token

    Assert.Contains("hello", actual.StandardOutput)
}

[<Fact>]
let ``Should execute buffered with separate encodings and forceful and graceful tokens`` () = task {
    use forceful = new CancellationTokenSource()
    use graceful = new CancellationTokenSource()

    let! actual =
        echoCommand
        |> Cli.Task.execBufferedf Encoding.UTF8 Encoding.UTF8 forceful.Token graceful.Token

    Assert.Contains("hello", actual.StandardOutput)
}

[<Fact>]
let ``Should match a successful result`` () = task {
    let! result = Command(Dummy.Program.FilePath).ExecuteAsync()

    match result with
    | CommandResult.Succeeded -> ()
    | CommandResult.Failed code -> failwith $"Expected success, got exit code {code}"
}

[<Fact>]
let ``Should match a failed result with its exit code`` () = task {
    let! result =
        Command(Dummy.Program.FilePath)
            .WithArguments([ "exit"; "3" ])
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()

    match result with
    | CommandResult.Succeeded -> failwith "Expected failure"
    | CommandResult.Failed code -> Assert.Equal(3, code)
}

[<Fact>]
let ``Should deconstruct a buffered result`` () = task {
    let! result = echoCommand.ExecuteBufferedAsync()

    let (CommandResult.Buffered(exitCode, stdout, stderr)) = result

    Assert.Equal(0, exitCode)
    Assert.Contains("hello", stdout)
    Assert.Equal(result.StandardOutput, stdout)
    Assert.Equal(result.StandardError, stderr)
}

[<Property>]
let ``Should configure stdin`` (value: NonNull<string>) =
    let input = PipeSource.FromString value.Get
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithStandardInputPipe(input)

    let actual = cmd |> Cli.stdin input

    expected.StandardInputPipe = actual.StandardInputPipe

[<Property>]
let ``Should configure stdout`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithStandardOutputPipe(pipe)

    let actual = cmd |> Cli.stdout pipe

    expected.StandardOutputPipe = actual.StandardOutputPipe

[<Property>]
let ``Should configure stderr`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithStandardErrorPipe(pipe)

    let actual = cmd |> Cli.stderr pipe

    expected.StandardOutputPipe = actual.StandardOutputPipe

[<Property>]
let ``Should configure target file`` (file: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithTargetFile(file.Get)

    let actual = cmd |> Cli.target file.Get

    expected.TargetFilePath = actual.TargetFilePath

[<Property>]
let ``Should configure validation`` () =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithValidation(CommandResultValidation.None)

    let actual = cmd |> Cli.validation CommandResultValidation.None

    expected.Validation = actual.Validation

[<Property>]
let ``Should configure working directory`` (dir: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath)
    let expected = cmd.WithWorkingDirectory(dir.Get)

    let actual = cmd |> Cli.workDir dir.Get

    expected.WorkingDirPath = actual.WorkingDirPath

[<Property>]
let ``Should convert to string`` (arg: NonNull<string>) =
    let cmd = Command(Dummy.Program.FilePath).WithArguments([ arg.Get ])
    let expected = cmd.ToString()

    let actual = cmd |> Cli.toString

    expected = actual
