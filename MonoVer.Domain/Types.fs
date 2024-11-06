namespace MonoVer.Domain.Types

open System.IO

type Csproj = Csproj of string
type Version =
    { Major: uint
      Minor: uint
      Patch: uint }
    
type Project = {
    Csproj: FileInfo
    CurrentVersion: Version
    Dependencies: Project list
}

type Projects = Project list
    
type SemVerImpact =
    | Major
    | Minor
    | Patch

type ChangesetId = Id of string

type RawChangeset = ChangesetId * string
type RawChangesets = RawChangeset list
type InvalidVersionFormat = InvalidVersionFormat of string

type TargetProject = TargetProject of string

type AffectedProject = {
      Project: TargetProject
      Impact: SemVerImpact
}

type ChangesetDescription =
                           | ChangesetDescription of string
                           | Empty
type ChangesetContent = {
      AffectedProjects: AffectedProject list
      Description: ChangesetDescription
}
type Changeset = {
    Id: ChangesetId
    Content: ChangesetContent
}

type ChangeDescription  = ChangeDescription of string

type ProjectChange =
    { ChangesetId: ChangesetId
      Project: Project
      Impact: SemVerImpact
      Description: ChangeDescription option
      DependencyGraph: Project list }

    