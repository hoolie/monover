namespace MonoVer

open System
open System.IO
open CommandLine
open MonoVer.Cli

[<Verb("init", HelpText= "initializes")>]
type InitOptions = {
    [<Option('w', "Workdir", Default = ".")>]
    Workdir: string
}


module InitCommand =
    let Run (): Result<unit,ApplicationError> =
        if File.Exists(".changesets/config.json") then
            Console.WriteLine("MonoVer is already initialized")
        else
            Directory.CreateDirectory(".changesets") |> ignore
            File.WriteAllText(".changeset/config.json", "{}")
        Ok ()

