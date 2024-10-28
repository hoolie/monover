module MonoVer.Publish

open System.IO
open MonoVer.ChangelogEntry
open MonoVer.Changeset
open MonoVer.ProjectStructure
open MonoVer.Version

type PublishResult =
    { Project: Project
      Changes: Descriptions
      NextVersion: Version }

type ProjectChange =
    { ChangesetId: ChangesetId
      Project: Project
      Impact: SemVerImpact
      Descriptions: Description list
      DependencyGraph: Project list }

let private dependsOn (dependant: Project) (project: Project) =
    project.Dependencies |> List.exists (fun x -> x.Csproj = dependant.Csproj)



let private findProjectByCsproj (projects: Project list) (csproj: TargetProject) =
    projects
    |> List.find (fun p -> p.Csproj.FullName = (FileInfo csproj.Path).FullName)

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

let private toChanges (cumulatedChanges) : PublishResult =
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
        mergeDescriptions (filteredChanges |> List.collect _.Descriptions)

    { Project = project
      NextVersion = nextVersion
      Changes = descriptions }

let Publish (projects: Project list) (changes: Changeset list) : PublishResult list =
    changes
    |> List.collect (splitChangeset projects)
    |> List.collect (publishTransientUpdates projects)
    |> List.groupBy (_.Project)
    |> List.map toChanges
