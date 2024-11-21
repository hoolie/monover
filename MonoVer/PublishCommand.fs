module MonoVer.PublishCommand

open System
open System.IO
open CommandLine
open FSharpPlus
open Microsoft.FSharp.Core
open MonoVer
open MonoVer.Changelog
open MonoVer.Cli
open MonoVer.Domain
open MonoVer.UpdateCsproj


type PublishDomainErrors =
    | MsProjectsError of MsProjectsError
    | UpdateVersionError of UpdateVersionError
    | PublishError of PublishError


[<Verb("publish", HelpText = "applies all open changesets.")>]
type PublishOptions =
    { [<Option(Default = ".")>]
      Workdir: string
      [<Option(Default = ".changesets")>]
      Changesets: string }


let CHANGELOG_TEMPLATE =
    """All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

"""

let ReadChangelogFor ( path: DirectoryInfo) : Changelog =
    let path = FileInfo(Path.Join(path.FullName, "Changelog.md"))
    { Path = path
      Content =
        match path.Exists with
        | false -> CHANGELOG_TEMPLATE
        | true  -> File.ReadAllText path.FullName }

let private loadRawChangesets =
    DirectoryInfo 
    >> _.EnumerateFiles("*.md")
    >> Seq.map (fun file -> ((ChangesetId.ChangesetId file.FullName), File.ReadAllText file.FullName))
    >> Seq.toList
    
let private loadSolution workdir =
    MsProjects.TryLoadFrom workdir
    |> Result.mapError MsProjectsError
   
let private updateVersion solution (newVersion: VersionIncreased) =
     Console.WriteLine $"Increase version of '{newVersion.Project}' to '{Version.ToString newVersion.Version}'"
     ApplyChanges solution newVersion
     |> Result.mapError PublishDomainErrors.UpdateVersionError
                                      
let private processChangesets projects = Changesets.Publish projects >> (Result.mapError PublishDomainErrors.PublishError)
let private updateChangelog (solution:MsProjects.MsSolution) ({Project = project; Changes = changes; Version = version}: NewChangelogEntry) =
        let project =
            Map.tryFind project solution
            |> Option.map _.FullPath
            |> Option.map FileInfo
            |> Option.map _.Directory
            |> Option.get // todo: this throws!
                        
        let changelogEntry:ChangelogVersionEntry = { Version = version; Date = DateOnly.FromDateTime DateTime.Today; Changes = changes }
        let oldChangelog = ReadChangelogFor project
        let newChangelog = AddEntry oldChangelog changelogEntry
        Console.WriteLine $"Write file '{newChangelog.Path.FullName}'"
        File.WriteAllText(newChangelog.Path.FullName, newChangelog.Content)
        Ok()
   
let private deleteChangeset (ChangesetId id) =
    Console.WriteLine $"Delete file '{id}'"
    Ok (
        //File.Delete id
        )
let RunPublish (args: PublishOptions) : Result<unit, ApplicationError> =
    let changesets = Path.Join(args.Workdir, args.Changesets)
    monad {
        // get all changesets
        let rawChangesets = loadRawChangesets changesets
        // get all projects
        let! solution = loadSolution args.Workdir
        let projects = Projects.FromSolution solution
                             
        // publish
        let! publishResult = processChangesets projects rawChangesets
        // execute changes
        return! publishResult
                        |> List.map (function
                            | NewChangelogEntry newEntry -> updateChangelog solution newEntry
                            | VersionIncreased projectVersion -> updateVersion solution projectVersion 
                            | ChangesetApplied changesetId -> deleteChangeset changesetId )
                        |> sequence
                        |>> ignore
    }
    |> Result.mapError (
        function
        | PublishError (FailedToParseChangeset (ChangesetId x,e)) -> CommandError (-4, $"""Failed to parse Changeset with name {x}.md:{e}""" )
        | MsProjectsError (SolutionFileNotFoundInWorkdir x) -> CommandError (-5, $"Could not find any solution file in working directory '{x}'") 
        | MsProjectsError (MultipleSolutionFilesFoundInWorkdir x) -> CommandError (-6, $"""Found multiple solution file in working directory: {x}""")
        | UpdateVersionError (FailedToUpdateVersionInFile e) -> CommandError (-7, $"Failed to update file {e.Project} to version {Version.ToString e.Version} ")
        )
