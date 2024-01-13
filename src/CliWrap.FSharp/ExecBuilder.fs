[<AutoOpen>]
module UnMango.CliWrap.FSharp.ExecBuilder

open System
open System.ComponentModel
open System.Threading
open CliWrap

type State =
    { Command: Command
      Cts: CancellationTokenSource }

    interface IDisposable with
        member this.Dispose() = this.Cts.Dispose()

module State =
    let initial name =
        { Command = Command(name)
          Cts = new CancellationTokenSource() }

type ExecBuilder(name: string) =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Yield(_: unit) = State.initial name

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    member _.Run(state: State) =
        state.Command.ExecuteAsync(state.Cts.Token)
        |> CommandTask.op_Implicit
        |> Async.AwaitTask

    [<CustomOperation("args")>]
    member _.Args(state: State, args: string) =
        { state with
            Command = state.Command.WithArguments(args) }

    [<CustomOperation("args")>]
    member _.Args(state: State, args: string seq) =
        { state with
            Command = state.Command.WithArguments(args) }

let exec name = ExecBuilder(name)
