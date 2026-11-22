module internal UnMango.CliWrap.FSharp.Internal

open System
open System.Runtime.ExceptionServices
open System.Threading.Tasks

module Async =
    /// Like Async.AwaitTask, but raises a lone inner exception instead of an AggregateException.
    let awaitTaskUnwrapped (task: Task<'T>) : Async<'T> = async {
        try
            return! Async.AwaitTask task
        with :? AggregateException as e when e.InnerExceptions.Count = 1 ->
            ExceptionDispatchInfo.Capture(e.InnerExceptions[0]).Throw()
            return Unchecked.defaultof<'T>
    }
