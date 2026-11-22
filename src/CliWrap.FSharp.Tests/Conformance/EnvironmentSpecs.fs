/// Conformance specs ported from upstream's CliWrap.Tests/EnvironmentSpecs.cs.
module Conformance.EnvironmentSpecs

open System
open System.IO
open CliWrap.Tests
open Xunit
open UnMango.CliWrap.FSharp
open Conformance.Support

[<Fact(Timeout = 15000)>]
let ``I can execute a command with a custom working directory`` () = task {
    use dir = new TempDirectory()

    let cmd = Cli.wrap Dummy.Program.FilePath |> Cli.arg "cwd" |> Cli.workDir dir.Path

    let! result = cmd |> Cli.execBuffered

    // macOS reports temp paths through the /private symlink.
    let normalize (path: string) =
        let full = Path.GetFullPath(path)

        if full.StartsWith("/private/", StringComparison.Ordinal) then
            full.Substring("/private".Length)
        else
            full

    Assert.Equal(normalize dir.Path, normalize (result.StandardOutput.Trim()))
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with additional environment variables`` () = task {
    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.args [ "env"; "foo"; "hello" ]
        |> Cli.env [ "foo", "bar"; "hello", "world" ]

    let! result = cmd |> Cli.execBuffered

    Assert.Equal<string list>([ "bar"; "world" ], lines result.StandardOutput)
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command with some environment variables overwritten`` () = task {
    let key = Guid.NewGuid()
    let variableToKeep = $"CLIWRAP_TEST_KEEP_%O{key}"
    let variableToOverwrite = $"CLIWRAP_TEST_OVERWRITE_%O{key}"
    let variableToUnset = $"CLIWRAP_TEST_UNSET_%O{key}"

    use _ = tempEnvironmentVariable variableToKeep "keep"
    use _ = tempEnvironmentVariable variableToOverwrite "overwrite"
    use _ = tempEnvironmentVariable variableToUnset "unset"

    let cmd =
        Cli.wrap Dummy.Program.FilePath
        |> Cli.args [ "env"; variableToKeep; variableToOverwrite; variableToUnset ]
        |> Cli.envf _.Set(variableToOverwrite, "overwritten").Set(variableToUnset, null)

    let! result = cmd |> Cli.execBuffered

    Assert.Equal<string list>([ "keep"; "overwritten" ], lines result.StandardOutput)
}
