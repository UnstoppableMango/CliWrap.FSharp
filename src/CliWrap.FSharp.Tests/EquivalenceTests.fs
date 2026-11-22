module EquivalenceTests

open System.Text
open CliWrap
open Xunit

let private target = "dummy"

[<Fact>]
let ``Should find no differences between identical configurations`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let a = Command(target).WithArguments("a b").WithStandardErrorPipe(pipe)
    let b = Command(target).WithArguments("a b").WithStandardErrorPipe(pipe)

    Assert.Empty(Equivalence.differences a b)

[<Fact>]
let ``Should report a different standard error pipe`` () =
    let a =
        Command(target).WithStandardErrorPipe(PipeTarget.ToStringBuilder(StringBuilder()))

    let b =
        Command(target).WithStandardErrorPipe(PipeTarget.ToStringBuilder(StringBuilder()))

    Assert.Equal<string list>([ "StandardErrorPipe" ], Equivalence.differences a b)
