module MonoVer.PublishCommand

open System
open System.IO
open CommandLine
open FSharpPlus
open FSharpPlus.Control
open Microsoft.Build.Construction
open Microsoft.FSharp.Core
open MonoVer.Changeset
open MonoVer.ProjectStructure
open MonoVer.Publish
open MonoVer.ChangelogEntry
open MonoVer.UpdateCsproj

type PublishDomainErrors =
    | FailedToParseChangeset of string
    | SolutionFileNotFoundInWorkdir of string
    | MultipleSolutionFilesFoundInWorkdir of string seq
    | UpdateVersionError of UpdateVersionError


[<Verb("publish", HelpText = "applies all open changesets.")>]
type PublishOptions =
    { [<Option(Default = ".")>]
      Workdir: string
      [<Option(Default = ".changesets")>]
      Changesets: string }


let ParseChangeset (fileInfo: FileInfo) : Result<Changeset, PublishDomainErrors> =
    let content = File.ReadAllText fileInfo.FullName

    match (ChangesetParser.Parse content) with
    | Error e -> Error(FailedToParseChangeset e)
    | Ok cs ->
        Ok
            { Id = (ChangesetId.Id fileInfo.Name)
              Content = cs }


let CHANGELOG_TEMPLATE =
    """All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

"""

let ReadChangelogFor (csproj: FileInfo) : Changelog =
    { Path = FileInfo(Path.Join(csproj.DirectoryName, "Changelog.md"))
      Content =
        match csproj.Directory.GetFiles("Changelog.md") |> Seq.tryHead with
        | None -> CHANGELOG_TEMPLATE
        | Some value -> File.ReadAllText value.FullName }

let RunPublish (args: PublishOptions) : Result<unit, PublishDomainErrors> =
    let changesets = Path.Join(args.Workdir, args.Changesets)
    // get all changesets
    monad {
        let! changesets =
            DirectoryInfo(changesets).EnumerateFiles("*.md")
            |> Seq.map ParseChangeset
            |> Seq.toList
            |> Sequence.Sequence

        // get all projects
        let! projects =
            match (DirectoryInfo(args.Workdir).GetFiles("*.sln") |> Array.map _.FullName) with
            | [||] -> Error(SolutionFileNotFoundInWorkdir args.Workdir)
            | [| x |] -> Ok(SlnParser.LoadProjects(SolutionFile.Parse(x)))
            | files -> Error(MultipleSolutionFilesFoundInWorkdir files)

        let parsedProjects = projects |> Map.values |> SlnParser.getProjects
        // publish
        let res = Publish parsedProjects changesets
        // update version
        let! _ = res
                 |>> (ApplyChanges projects )
                 |> sequence
                 |>> ignore
                 |> Result.mapError PublishDomainErrors.UpdateVersionError
                 


        // update/create Changelogs
        let changelogs =
            res
            |> List.map (fun x ->
                (ReadChangelogFor x.Project.Csproj,
                 { Version = x.NextVersion
                   Date = DateOnly.FromDateTime DateTime.Today
                   Changes = x.Changes }))
            |> List.map (fun (x, y) -> AddEntryToChangelog y x)

        for changelog in changelogs do
            File.WriteAllText(changelog.Path.FullName, changelog.Content)

        return ()
    }
