/// Stand-ins for the PowerKit and Utils helpers upstream's specs use.
module Conformance.Support

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open Xunit

/// Whether a process with this id is still running.
let isRunning (processId: int) =
    try
        use p = Process.GetProcessById(processId)
        not p.HasExited
    with
    | :? ArgumentException
    | :? InvalidOperationException -> false

/// Fails unless running the action is canceled by this token.
let assertCanceledBy (token: CancellationToken) (run: unit -> Task) = task {
    let! error = Assert.ThrowsAnyAsync<OperationCanceledException>(run)
    Assert.Equal(token, error.CancellationToken)
}

/// Splits text into its non-empty lines, like upstream's `ConsistOfLines` assertion.
let lines (text: string) =
    text.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> List.ofArray

/// Sets an environment variable for this process until the result is disposed.
let tempEnvironmentVariable (name: string) (value: string) =
    let last = Environment.GetEnvironmentVariable(name)
    Environment.SetEnvironmentVariable(name, value)

    { new IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable(name, last)
    }

/// Appends a directory to PATH until the result is disposed.
let extendPath (path: string) =
    tempEnvironmentVariable "PATH" (Environment.GetEnvironmentVariable("PATH") + string Path.PathSeparator + path)

/// A directory under the system temp folder that is deleted when the result is disposed.
type TempDirectory() =
    let path = Directory.CreateTempSubdirectory("CliWrap.FSharp.Tests-").FullName

    member _.Path = path

    interface IDisposable with
        member _.Dispose() =
            try
                Directory.Delete(path, true)
            with :? IOException ->
                ()
