// For more information see https://aka.ms/fsharp-console-apps

open System
open CommandLine
open Microsoft.Build.Locator
open MonoVer
open MonoVer.Cli
open MonoVer.PublishCommand

MSBuildLocator.RegisterDefaults() |> ignore

let printError (error: ApplicationError) =
    match error with
    | MissingCommand _ -> -1
    | NotImplemented s ->
        Console.WriteLine($"The command '{s}' is not yet implemented")
        -2
    | CommandError(code, description) ->
        Console.WriteLine(description)
        code

let runCommand (command: Parsed<obj>) =
    let result: Result<_, ApplicationError> =
        match command.Value with
        | :? PublishOptions as opts -> (RunPublish opts)
        | :? NewChangesetOption as opts -> CreateChangesetCommand.Run opts
        | :? InitOptions -> InitCommand.Run()
        | _ -> Result.Error(NotImplemented command.TypeInfo.Current.Name)

    match result with
    | Error e -> printError e
    | _ -> 0

[<EntryPoint>]
let main argv =
    let options =
        Parser.Default.ParseArguments<PublishOptions, NewChangesetOption, InitOptions> argv

    match options with
    | :? Parsed<obj> as command -> runCommand command
    | :? NotParsed<obj> as p -> printError (MissingCommand())
    | _ -> -1
