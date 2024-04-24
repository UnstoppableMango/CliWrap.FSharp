namespace UnMango.CliWrap.FSharp.Tests.Dummy

open System.CommandLine
open System.IO
open System.Reflection
open System.Runtime.InteropServices

type Program =
    static member FilePath =
        Path.ChangeExtension(
            Assembly.GetExecutingAssembly().Location,
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                "exe"
            else
                null
        )

module Program =
    let command = RootCommand("Dummy program for testing")

    [<EntryPoint>]
    let main args = command.Invoke args
