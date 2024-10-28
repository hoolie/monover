// For more information see https://aka.ms/fsharp-console-apps

open System
open CommandLine
open Microsoft.Build.Locator
open MonoVer.Cli
open MonoVer.PublishCommand

MSBuildLocator.RegisterDefaults() |> ignore
let helpText = "This section should be helpful soon."
let printError (error: ApplicationError) =
    let errorCode,errorText =
        match error with
        | MissingCommand _ -> (-1, "No command given. Please use a command as the first argument for the call.")
        | UnknownCommand s -> (-2, $"'{s}' was not recognized as a valid command")
        | PublishDomainErrors e -> (-3, $"Failed to publish: {e}") 
    Console.WriteLine(errorText)
    Console.WriteLine(helpText)
    errorCode

let runCommand( command: Parsed<obj>) =
    let result: Result<_,ApplicationError> =
        match command.Value with
        | :? PublishOptions as opts -> (RunPublish opts|> Result.mapError ApplicationError.PublishDomainErrors)
        | _ -> Result.Error (UnknownCommand command.TypeInfo.Current.Name )
    match result with
        | Error e -> printError e
        | _ -> 0
    
[<EntryPoint>]
let main argv =
    let options = Parser.Default.ParseArguments<PublishOptions,Object> argv
    match options with
        | :? CommandLine.Parsed<obj> as command -> runCommand command
        | :? NotParsed<obj> as p -> printError (MissingCommand())
        | _ ->  -1
// let markdown = """
// ---
// "projectA": patch
// "projectB": patch
// ---
// # Added
// bla
// blub
// # Fixed
// blub
// """
// let result = MonoVer.ChangesetParser.Parse markdown
//
// match result with
// | Ok changeset ->
//     printfn $"Parsed successfully: %A{changeset}"
// | Error errorMsg ->
//     printfn $"Parsing failed: %s{errorMsg}"
// printfn "hi"