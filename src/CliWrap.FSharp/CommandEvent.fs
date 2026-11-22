/// <summary>
/// Pattern matching over the events produced by an event stream (see <c>Cli.observe</c> and <c>Cli.listen</c>).
/// </summary>
[<AutoOpen>]
module UnMango.CliWrap.FSharp.CommandEvent

open System
open CliWrap.EventStream

/// <summary>
/// Matches a <see cref="CommandEvent"/> by its kind.
/// </summary>
/// <param name="event">The event to match.</param>
/// <returns>
/// <c>Started</c> with the process ID, <c>StdOut</c> or <c>StdErr</c> with the line of text,
/// or <c>Exited</c> with the exit code.
/// </returns>
/// <exception cref="ArgumentException">
/// The event is a subclass of <see cref="CommandEvent"/> that CliWrap does not produce.
/// </exception>
/// <example>
/// <code>
/// match event with
/// | Started pid -> printfn $"Started %d{pid}"
/// | StdOut line -> printfn $"out: %s{line}"
/// | StdErr line -> eprintfn $"err: %s{line}"
/// | Exited code -> printfn $"Exited with %d{code}"
/// </code>
/// </example>
let (|Started|StdOut|StdErr|Exited|) (event: CommandEvent) =
    match event with
    | null -> nullArg (nameof event)
    | :? StartedCommandEvent as e -> Started e.ProcessId
    | :? StandardOutputCommandEvent as e -> StdOut e.Text
    | :? StandardErrorCommandEvent as e -> StdErr e.Text
    | :? ExitedCommandEvent as e -> Exited e.ExitCode
    | _ -> raise (ArgumentException($"Unknown command event type: {event.GetType().FullName}", nameof event))
