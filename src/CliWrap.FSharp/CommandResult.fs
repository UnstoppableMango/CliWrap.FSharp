/// Active patterns for matching on command results.
module UnMango.CliWrap.FSharp.CommandResult

open CliWrap
open CliWrap.Buffered

/// <summary>
/// Matches a command result by whether it exited successfully.
/// <c>Failed</c> carries the non-zero exit code.
/// </summary>
let (|Succeeded|Failed|) (result: CommandResult) =
    if result.IsSuccess then
        Succeeded
    else
        Failed result.ExitCode

/// <summary>
/// Breaks a buffered command result into its exit code, standard output and standard error.
/// </summary>
let (|Buffered|) (result: BufferedCommandResult) =
    (result.ExitCode, result.StandardOutput, result.StandardError)
