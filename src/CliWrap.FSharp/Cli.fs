module UnMango.CliWrap.FSharp.Cli

open CliWrap

let exec (command: Command) =
    command.ExecuteAsync() |> CommandTask.op_Implicit |> Async.AwaitTask

module C =
    let exec (command: Command) cancellationToken = command.ExecuteAsync(cancellationToken)

    let execf (command: Command) forceful graceful =
        command.ExecuteAsync(graceful, forceful)
