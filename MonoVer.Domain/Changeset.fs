namespace MonoVer.Domain

open System.IO
open MonoVer.Domain.Types

module Changeset =

    open FParsec
    open MonoVer.Domain.Types

    let private pImpact =
        choice
            [ pstringCI "major" >>% SemVerImpact.Major
              pstringCI "minor" >>% SemVerImpact.Minor
              pstringCI "patch" >>% SemVerImpact.Patch ]

    let private pHorizontalLine = pstring "---" .>> many (pchar '-') .>> spaces

    let private pSectionHeading name =
        spaces >>. pstring ("# " + name) .>> spaces

    // Parser for a project name (quoted string)
    let private projectName =
        between (pchar '"') (pchar '"') (manyChars (noneOf "\"")) .>> spaces

    // Parser for a single line in the format: "ProjectName": status
    let private pAffectedProject =
        projectName .>>. (pchar ':' >>. spaces >>. pImpact)
        |>> (fun (name, impact) ->
            { Project = TargetProject name
              Impact = impact })

    // Parser for multiple lines of project impact
    let private pAffectedProjects = many (pAffectedProject .>> spaces)

    // Parser for the entire document including the dashes
    let private pAffectedProjectsSection =
        between pHorizontalLine pHorizontalLine pAffectedProjects

    // Parser for description lines (e.g., "Added some new features")
    let private pLine = many1Satisfy (fun c -> c <> '\n') .>> newline
    // Parser for description header (e.g., "# Added")
    let private pHeader = pchar '#' >>. spaces >>. pLine

    // Parser for a section of descriptions (e.g., "# Added\nDescription")
    let private pDescriptionSection =
        pipe2 pHeader (manyTill pLine (lookAhead (pHeader <|> (eof >>% "")))) (fun header lines ->
            match header with
            | "Added" -> Added(lines)
            | "Changed" -> Changed(lines)
            | "Deprecated" -> Deprecated(lines)
            | "Removed" -> Removed(lines)
            | "Fixed" -> Fixed(lines)
            | "Security" -> Security(lines)
            | _ -> failwith "Unknown description header")

    // Parser for all sections of descriptions
    let private pDescriptions = many1 pDescriptionSection

    // Parser for the entire document
    let private pChangeset =
        pAffectedProjectsSection .>>. pDescriptions
        |>> (fun (projects, descriptions) ->
            { AffectedProjects = projects
              Descriptions = descriptions })

    // Parser for all sections of descriptions
    let Parse markdown : Result<ChangesetContent, string> =
        match run pChangeset markdown with
        | Success(changeset, _, _) -> Result.Ok changeset
        | Failure(errorMessage, _, _) -> Result.Error errorMessage

    let ParseRaw ((id, content): RawChangeset) : Result<Changeset, PublishError> =
        content
        |> Parse
        |> Result.mapError (fun e -> FailedToParseChangeset(id, e))
        |> Result.map (fun parsed -> { Id = id; Content = parsed })

module Changesets =
    let ParseRaw = List.map Changeset.ParseRaw >> FSharpPlus.Operators.sequence

    let private dependsOn (dependant: Project) (project: Project) =
        project.Dependencies |> List.exists (fun x -> x.Csproj = dependant.Csproj)

    let private findProjectByCsproj (projects: Project list) (TargetProject csproj: TargetProject) =
        projects |> List.find (fun p -> p.Csproj.FullName = (FileInfo csproj).FullName)

    let private projectChangeFromAffectedProject
        (projects: Project list)
        (changeset: Changeset)
        (affectedProject: AffectedProject)
        =
        let project = findProjectByCsproj projects affectedProject.Project

        { ChangesetId = changeset.Id
          Project = project
          Impact = affectedProject.Impact
          Descriptions = changeset.Content.Descriptions
          DependencyGraph = [ project ] }

    let private splitChangeset (projects: Project list) (changeset: Changeset) : ProjectChange list =
        changeset.Content.AffectedProjects
        |> List.map (projectChangeFromAffectedProject projects changeset)

    let rec publishTransientUpdates (projects: Project list) (projectChange: ProjectChange) : ProjectChange list =
        let updateProject project =
            let projectChange =
                { projectChange with
                    Project = project
                    Descriptions = [ Changed(List.singleton $"updated dependency {projectChange.Project.Csproj}") ]
                    DependencyGraph = project :: projectChange.DependencyGraph }

            projectChange :: (publishTransientUpdates projects projectChange)

        projectChange
        :: (projects
            |> List.filter (dependsOn projectChange.Project)
            |> List.collect updateProject)



    let private closestChange ((_: ChangesetId, changes: ProjectChange list)) =
        changes |> List.sortBy (_.DependencyGraph.Length) |> List.head

    let private toChanges cumulatedChanges : PublishEvent list =
        let (project: Project, changes: ProjectChange list) = cumulatedChanges

        let filteredChanges =
            changes |> List.groupBy (_.ChangesetId) |> List.map closestChange

        let highestImpact = filteredChanges |> List.map (_.Impact) |> List.sort |> List.head

        let nextVersion =
            match highestImpact with
            | SemVerImpact.Major ->
                { project.CurrentVersion with
                    Major = project.CurrentVersion.Major + 1u
                    Minor = 0u
                    Patch = 0u }
            | SemVerImpact.Minor ->
                { project.CurrentVersion with
                    Minor = project.CurrentVersion.Minor + 1u
                    Patch = 0u }
            | SemVerImpact.Patch ->
                { project.CurrentVersion with
                    Patch = project.CurrentVersion.Patch + 1u }
            | _ -> System.ArgumentOutOfRangeException() |> raise


        let descriptions =
            Descriptions.merge (filteredChanges |> List.collect _.Descriptions)

        [ NewChangelogEntry
              { Project = Csproj project.Csproj.FullName
                Changes = descriptions
                Version = nextVersion }
          VersionIncreased
              { Project = Csproj project.Csproj.FullName
                Version = nextVersion } ]
    open FSharpPlus
    let Publish projects : ProcessChangesets =
        fun rawChangesets -> 
            (ParseRaw rawChangesets
            |>> (fun changesets ->
                (List.collect(splitChangeset projects) changesets
                >>= publishTransientUpdates projects
                |> groupBy (_.Project)
                >>= toChanges)
                @ (rawChangesets |>> fst |>> ChangesetApplied))
            )
            
            
