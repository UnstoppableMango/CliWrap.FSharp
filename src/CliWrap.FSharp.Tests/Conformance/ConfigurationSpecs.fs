/// Conformance specs ported from upstream's CliWrap.Tests/ConfigurationSpecs.cs.
module Conformance.ConfigurationSpecs

open System.Diagnostics
open System.IO
open CliWrap
open Xunit
open UnMango.CliWrap.FSharp

let private changed original modified = Equivalence.differences original modified

[<Fact>]
let ``I can create a command with the default configuration`` () =
    let cmd = Cli.wrap "foo"

    Assert.Equal("foo", cmd.TargetFilePath)
    Assert.Empty(cmd.Arguments)
    Assert.Equal(Directory.GetCurrentDirectory(), cmd.WorkingDirPath)
    Assert.Same(ResourcePolicy.Default, cmd.ResourcePolicy)
    Assert.Same(Credentials.Default, cmd.Credentials)
    Assert.Empty(cmd.EnvironmentVariables)
    Assert.Equal(CommandResultValidation.ZeroExitCode, cmd.Validation)
    Assert.Same(PipeSource.Null, cmd.StandardInputPipe)
    Assert.Same(PipeTarget.Null, cmd.StandardOutputPipe)
    Assert.Same(PipeTarget.Null, cmd.StandardErrorPipe)

[<Fact>]
let ``I can configure the target file`` () =
    let original = Cli.wrap "foo"

    let modified = original |> Cli.target "bar"

    Assert.Equal<string list>([ "TargetFilePath" ], changed original modified)
    Assert.Equal("bar", modified.TargetFilePath)

[<Fact>]
let ``I can configure the command line arguments`` () =
    let original = Cli.wrap "foo" |> Cli.arg "xxx"

    let modified = original |> Cli.arg "qqq ppp"

    Assert.Equal<string list>([ "Arguments" ], changed original modified)
    Assert.Equal("qqq ppp", modified.Arguments)

[<Fact>]
let ``I can configure the command line arguments by passing an array`` () =
    let original = Cli.wrap "foo" |> Cli.arg "xxx"

    let modified = original |> Cli.args [ "-a"; "foo bar" ]

    Assert.Equal<string list>([ "Arguments" ], changed original modified)
    Assert.Equal("-a \"foo bar\"", modified.Arguments)

[<Fact>]
let ``I can configure the command line arguments using a builder`` () =
    let original = Cli.wrap "foo" |> Cli.arg "xxx"

    let modified =
        original
        |> Cli.argsf (fun b ->
            b
                .Add("-a")
                .Add("foo bar")
                .Add("\"foo\\\\bar\"")
                .Add(3.14)
                .Add([ "foo"; "bar" ])
                .Add([ box -5; box 89.13 ] |> Seq.map (fun v -> v :?> System.IFormattable)))

    Assert.Equal<string list>([ "Arguments" ], changed original modified)
    Assert.Equal("-a \"foo bar\" \"\\\"foo\\\\bar\\\"\" 3.14 foo bar -5 89.13", modified.Arguments)

[<Fact>]
let ``I can configure the working directory`` () =
    let original = Cli.wrap "foo" |> Cli.workDir "xxx"

    let modified = original |> Cli.workDir "new"

    Assert.Equal<string list>([ "WorkingDirPath" ], changed original modified)
    Assert.Equal("new", modified.WorkingDirPath)

let private expectedPolicy () =
    ResourcePolicy(ProcessPriorityClass.High, 0x1n, 1024n, 2048n)

let private policyValues (p: ResourcePolicy) =
    p.Priority, p.Affinity, p.MinWorkingSet, p.MaxWorkingSet

[<Fact>]
let ``I can configure the resource policy`` () =
    let original = Cli.wrap "foo" |> Cli.resourcePolicy ResourcePolicy.Default

    let modified = original |> Cli.resourcePolicy (expectedPolicy ())

    Assert.Equal<string list>([ "ResourcePolicy" ], changed original modified)
    Assert.Equal(policyValues (expectedPolicy ()), policyValues modified.ResourcePolicy)

[<Fact>]
let ``I can configure the resource policy using a builder`` () =
    let original = Cli.wrap "foo" |> Cli.resourcePolicy ResourcePolicy.Default

    let modified =
        original
        |> Cli.resourcePolicyf (fun b ->
            b
                .SetPriority(ProcessPriorityClass.High)
                .SetAffinity(0x1n)
                .SetMinWorkingSet(1024n)
                .SetMaxWorkingSet(2048n))

    Assert.Equal<string list>([ "ResourcePolicy" ], changed original modified)
    Assert.Equal(policyValues (expectedPolicy ()), policyValues modified.ResourcePolicy)

let private credentialValues (c: Credentials) =
    c.Domain, c.UserName, c.Password, c.LoadUserProfile

[<Fact>]
let ``I can configure the user credentials`` () =
    let original = Cli.wrap "foo" |> Cli.creds (Credentials("xxx", "xxx", "xxx"))

    let modified =
        original |> Cli.creds (Credentials("domain", "username", "password", true))

    Assert.Equal<string list>([ "Credentials" ], changed original modified)
    Assert.Equal(("domain", "username", "password", true), credentialValues modified.Credentials)

[<Fact>]
let ``I can configure the user credentials using a builder`` () =
    let original = Cli.wrap "foo" |> Cli.creds (Credentials("xxx", "xxx", "xxx"))

    let modified =
        original
        |> Cli.credsf _.SetDomain("domain").SetUserName("username").SetPassword("password").LoadUserProfile()

    Assert.Equal<string list>([ "Credentials" ], changed original modified)
    Assert.Equal(("domain", "username", "password", true), credentialValues modified.Credentials)

let private environment (cmd: Command) =
    cmd.EnvironmentVariables |> Seq.map (fun kv -> kv.Key, kv.Value) |> Set.ofSeq

[<Fact>]
let ``I can configure the environment variables`` () =
    let original = Cli.wrap "foo" |> Cli.envf _.Set("xxx", "xxx")

    let modified = original |> Cli.env [ "name", "value"; "key", "door" ]

    Assert.Equal<string list>([ "EnvironmentVariables" ], changed original modified)
    Assert.Equal<Set<string * string>>(set [ "name", "value"; "key", "door" ], environment modified)

[<Fact>]
let ``I can configure the environment variables using a builder`` () =
    let original = Cli.wrap "foo" |> Cli.envf _.Set("xxx", "xxx")

    let modified =
        original
        |> Cli.envf (fun b ->
            b
                .Set("name", "value")
                .Set("key", "door")
                .Set(dict [ "zzz", "yyy"; "aaa", "bbb" ]))

    Assert.Equal<string list>([ "EnvironmentVariables" ], changed original modified)

    Assert.Equal<Set<string * string>>(
        set [ "name", "value"; "key", "door"; "zzz", "yyy"; "aaa", "bbb" ],
        environment modified
    )

[<Fact>]
let ``I can configure the result validation strategy`` () =
    let original = Cli.wrap "foo" |> Cli.validation CommandResultValidation.ZeroExitCode

    let modified = original |> Cli.validation CommandResultValidation.None

    Assert.Equal<string list>([ "Validation" ], changed original modified)
    Assert.Equal(CommandResultValidation.None, modified.Validation)

[<Fact>]
let ``I can configure the stdin pipe`` () =
    let original = Cli.wrap "foo" |> Cli.stdin PipeSource.Null

    let modified = original |> Cli.stdin (ReadFrom.string "new")

    Assert.Equal<string list>([ "StandardInputPipe" ], changed original modified)

[<Fact>]
let ``I can configure the stdout pipe`` () =
    let original = Cli.wrap "foo" |> Cli.stdout PipeTarget.Null

    let modified = original |> Cli.stdout PipeTo.nullStream

    Assert.Equal<string list>([ "StandardOutputPipe" ], changed original modified)

[<Fact>]
let ``I can configure the stderr pipe`` () =
    let original = Cli.wrap "foo" |> Cli.stderr PipeTarget.Null

    let modified = original |> Cli.stderr PipeTo.nullStream

    Assert.Equal<string list>([ "StandardErrorPipe" ], changed original modified)
