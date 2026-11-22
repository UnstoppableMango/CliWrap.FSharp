module internal UnMango.CliWrap.FSharp.Internal

open System
open System.Runtime.ExceptionServices
open System.Threading.Tasks

module Async =
    /// Like Async.AwaitTask, but a task that faults with a single exception raises that
    /// exception rather than an AggregateException, the way awaiting the task in C# does.
    let awaitTaskUnwrapped (task: Task<'T>) : Async<'T> = async {
        try
            return! Async.AwaitTask task
        with :? AggregateException as e when e.InnerExceptions.Count = 1 ->
            ExceptionDispatchInfo.Capture(e.InnerExceptions[0]).Throw()
            return Unchecked.defaultof<'T>
    }
