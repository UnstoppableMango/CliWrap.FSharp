module EquivalenceTests

open System.Text
open CliWrap
open Xunit

let private target = "dummy"

[<Fact>]
let ``Should find no differences between identical configurations`` () =
    let pipe = PipeTarget.ToStringBuilder(StringBuilder())
    let expected = Command(target).WithArguments("a b").WithStandardErrorPipe(pipe)
    let actual = Command(target).WithArguments("a b").WithStandardErrorPipe(pipe)

    Assert.Empty(Equivalence.differences expected actual)

[<Fact>]
let ``Should report a different standard error pipe`` () =
    let expected =
        Command(target).WithStandardErrorPipe(PipeTarget.ToStringBuilder(StringBuilder()))

    let actual =
        Command(target).WithStandardErrorPipe(PipeTarget.ToStringBuilder(StringBuilder()))

    Assert.Equal<string list>([ "StandardErrorPipe" ], Equivalence.differences expected actual)
