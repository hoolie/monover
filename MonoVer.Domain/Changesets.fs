namespace MonoVer.Domain

type Project =
    { Id: ProjectId
      CurrentVersion: VersionPrefix
      Dependencies: Project list }

type Projects = Project list

type RawChangesets = RawChangeset list

type NewChangelogEntry =
    { Project: ProjectId
      Changes: ChangeDescriptions
      Version: Version }

type VersionIncreased =
    { Project: ProjectId; Version: Version }


type PublishEvent =
    | NewChangelogEntry of NewChangelogEntry
    | VersionIncreased of VersionIncreased
    | ChangesetApplied of ChangesetId

type PublishResult = Result<PublishEvent list, PublishError>
type ProcessChangesets = RawChangesets -> PublishResult

module Changesets =


    type private ChangeType =
        | Explicit
        | Transient

    type private ProjectChange =
        { ChangesetId: ChangesetId
          Project: Project
          Impact: SemVerImpact
          Description: ChangeDescription option
          ChangeType: ChangeType }

    let private ParseRaw projectIds =
        List.map (Changeset.ParseRaw projectIds) >> FSharpPlus.Operators.sequence

    let private dependsOn (dependant: Project) (project: Project) =
        project.Dependencies |> List.exists (fun x -> x.Id = dependant.Id)

    let private findProjectByCsproj (projects: Project list) (projectId: ProjectId) =
        projects |> List.find (fun p -> p.Id = projectId)

    let private projectChangeFromAffectedProject
        (projects: Project list)
        (changeset: ValidChangeset)
        (affectedProject: AffectedProject)
        =
        let project = findProjectByCsproj projects affectedProject.Project

        let description =
            match changeset.Description with
            | ChangesetDescription desc -> Some(ChangeDescription desc)
            | Empty -> None

        { ChangesetId = changeset.Id
          Project = project
          Impact = affectedProject.Impact
          Description = description
          ChangeType = Explicit }

    let private splitChangeset (projects: Project list) (changeset: ValidChangeset) : ProjectChange list =
        changeset.AffectedProjects
        |> List.map (projectChangeFromAffectedProject projects changeset)

    let rec private publishTransientUpdates
        (projects: Project list)
        (projectChange: ProjectChange)
        : ProjectChange list =
        let updateProject project =
            let (ProjectId projectId) = projectChange.Project.Id
            let changesetId = projectChange.ChangesetId

            let projectChange =
                { ChangesetId = changesetId
                  Impact = Patch
                  Project = project
                  Description = Some(ChangeDescription $"updated dependency {projectId}")
                  ChangeType = Transient }

            projectChange :: (publishTransientUpdates projects projectChange)

        projectChange
        :: (projects
            |> List.filter (dependsOn projectChange.Project)
            |> List.collect updateProject)



    let private overrideTransientChanges ((_: ChangesetId, changes: ProjectChange list)) =
        match (changes |> List.forall (fun t -> t.ChangeType = Transient)) with
        | true -> changes
        | _ -> changes |> List.filter (fun c -> c.ChangeType = Explicit)

    let private toDescriptionWithImpact (change: ProjectChange) : DescriptionWithImpact option =
        change.Description
        |> Option.map (fun desc ->
            { Impact = change.Impact
              Description = desc })

    let private collectChanges versionSuffix cumulatedChanges : PublishEvent list =
        let (project: Project, changes: ProjectChange list) = cumulatedChanges

        let filteredChanges =
            changes
            |> List.distinct
            |> List.groupBy (_.ChangesetId)
            |> List.collect overrideTransientChanges


        let descriptionsByImpact =
            filteredChanges
            |> List.map toDescriptionWithImpact
            |> List.collect Option.toList

        let highestImpact = filteredChanges |> List.map (_.Impact) |> List.sort |> List.head

        let nextVersionPrefix =
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

        let nextVersion = Version.Create nextVersionPrefix versionSuffix

        [ NewChangelogEntry
              { Project = project.Id
                Changes = ChangeDescriptions.Create descriptionsByImpact
                Version = nextVersion }
          VersionIncreased
              { Project = project.Id
                Version = nextVersion } ]

    open FSharpPlus

    let Publish projects versionSuffix : ProcessChangesets =
        let projectIds = (projects |>> _.Id)
        let parseChangesets = ParseRaw projectIds
        let splitChangeset = splitChangeset projects
        let publishTransientUpdates = publishTransientUpdates projects
        let groupByProject (changes: ProjectChange list) = changes |> List.groupBy (_.Project)
        let collectChanges = collectChanges versionSuffix

        fun rawChangesets ->
            monad {
                let! changesets = parseChangesets rawChangesets

                let changeEvents =
                    changesets
                    >>= splitChangeset
                    >>= publishTransientUpdates
                    |> groupByProject
                    >>= collectChanges

                let appliedChangesetEvents = (rawChangesets |>> fst |>> ChangesetApplied)
                changeEvents @ appliedChangesetEvents
            }
