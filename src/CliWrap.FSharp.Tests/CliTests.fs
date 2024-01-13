module CliTests

open CliWrap
open FsCheck.Xunit
open UnMango.CliWrap.FSharp

[<Property>]
let ``Should create command`` target =
    let expected = Command(target)

    let actual = Cli.wrap target

    expected.TargetFilePath = actual.TargetFilePath
