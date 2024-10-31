module MonoVer.Domain.Changesets

open System.IO
open MonoVer.Domain.Types

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
                Descriptions = [ Changed(List.singleton $"updated dependency {projectChange.Project.Csproj.Name}") ]
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
        
        
