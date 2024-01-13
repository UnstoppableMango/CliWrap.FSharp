module UnMango.CliWrap.FSharp.PipeBuilder

open System.ComponentModel

type PipeBuilder() =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run() = ()

let pipeline = PipeBuilder()
