namespace UnMango.CliWrap.FSharp

open System
open CliWrap

module PipeTo =
    let stdout = Console.OpenStandardOutput() |> PipeTarget.ToStream
    let stderr = Console.OpenStandardError() |> PipeTarget.ToStream

module ReadFrom =
    let stdin = Console.OpenStandardInput() |> PipeSource.FromStream
