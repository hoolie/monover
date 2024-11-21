namespace MonoVer.Domain

type Project = {
    Csproj: ProjectId
    CurrentVersion: Version
    Dependencies: Project list
}

type Projects = Project list
    
type RawChangesets = RawChangeset list



type ProjectChange =
    { ChangesetId: ChangesetId
      Project: Project
      Impact: SemVerImpact
      Description: ChangeDescription option
      DependencyGraph: Project list }

    


type NewChangelogEntry =
    { Project: ProjectId
      Changes: ChangeDescriptions
      Version: Version }

type VersionIncreased = { Project: ProjectId; Version: Version }


type PublishEvent =
    | NewChangelogEntry of NewChangelogEntry
    | VersionIncreased of VersionIncreased
    | ChangesetApplied of ChangesetId

type MergedChangesets = Result<PublishEvent list, PublishError>
type ProcessChangesets = RawChangesets -> MergedChangesets
module Changesets = 


    let ParseRaw = List.map Changeset.ParseRaw >> FSharpPlus.Operators.sequence

    let private dependsOn (dependant: Project) (project: Project) =
        project.Dependencies |> List.exists (fun x -> x.Csproj = dependant.Csproj)

    let private findProjectByCsproj (projects: Project list) (projectId: ProjectId) =
        // todo: what if ProjectNotFound? This can either be done while parsing, or here with try find, or maybe both.
        projects |> List.find (fun p -> p.Csproj = projectId)

    let private projectChangeFromAffectedProject
        (projects: Project list)
        (changeset: Changeset)
        (affectedProject: AffectedProject)
        =
        let project = findProjectByCsproj projects affectedProject.Project
        let description =
            match changeset.Content.Description with
            | ChangesetDescription desc -> Some (ChangeDescription desc)
            | Empty -> None

        { ChangesetId = changeset.Id
          Project = project
          Impact = affectedProject.Impact
          Description = description
          DependencyGraph = [ project ] }

    let private splitChangeset (projects: Project list) (changeset: Changeset) : ProjectChange list =
        changeset.Content.AffectedProjects
        |> List.map (projectChangeFromAffectedProject projects changeset)

    let rec publishTransientUpdates (projects: Project list) (projectChange: ProjectChange) : ProjectChange list =
        let updateProject project =
            let (ProjectId projectId) = projectChange.Project.Csproj
            let projectChange =
                { projectChange with
                    Impact = Patch
                    Project = project
                    Description = Some (ChangeDescription $"updated dependency { projectId }")
                    DependencyGraph = project :: projectChange.DependencyGraph }

            projectChange :: (publishTransientUpdates projects projectChange)

        projectChange
        :: (projects
            |> List.filter (dependsOn projectChange.Project)
            |> List.collect updateProject)



    let private closestChange ((_: ChangesetId, changes: ProjectChange list)) =
        changes |> List.sortBy (_.DependencyGraph.Length) |> List.head

    let private toDescriptionWithImpact(change: ProjectChange): DescriptionWithImpact option=
        change.Description
        |> Option.map(fun desc -> {Impact = change.Impact; Description = desc })
        
    let private toChanges cumulatedChanges : PublishEvent list =
        let (project: Project, changes: ProjectChange list) = cumulatedChanges

        let filteredChanges =
            changes |> List.groupBy (_.ChangesetId) |> List.map closestChange
        let descriptionsByImpact =
            filteredChanges
            |> List.map toDescriptionWithImpact
            |> List.collect Option.toList

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


        [ NewChangelogEntry
              { Project =  project.Csproj
                Changes = ChangeDescriptions.Create descriptionsByImpact
                Version = nextVersion }
          VersionIncreased
              { Project =  project.Csproj
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
            
            
