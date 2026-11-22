/// Conformance specs ported from upstream's CliWrap.Tests/PathResolutionSpecs.cs.
module Conformance.PathResolutionSpecs

open System
open System.IO
open Xunit
open UnMango.CliWrap.FSharp
open Conformance.Support

[<Fact(Timeout = 15000)>]
let ``I can execute a command on an executable using its short name`` () = task {
    let cmd = Cli.wrap "dotnet" |> Cli.arg "--version"

    let! result = cmd |> Cli.execBuffered

    Assert.True(result.IsSuccess)
    Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$", result.StandardOutput.Trim())
}

[<Fact(Timeout = 15000)>]
let ``I can execute a command on a script using its short name`` () = task {
    Assert.SkipUnless(OperatingSystem.IsWindows(), "Path resolution for scripts is only required on Windows.")

    use dir = new TempDirectory()
    do! File.WriteAllTextAsync(Path.Combine(dir.Path, "test-script.cmd"), "@echo hello")
    use _ = extendPath dir.Path

    let! result = Cli.wrap "test-script" |> Cli.execBuffered

    Assert.True(result.IsSuccess)
    Assert.Equal("hello", result.StandardOutput.Trim())
}
