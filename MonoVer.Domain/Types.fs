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

    
type Descriptions = {
    Added: string list
    Changed: string list
    Deprecated: string list
    Removed: string list
    Fixed: string list
    Security: string list
}

    
type SemVerImpact =
    | Major
    | Minor
    | Patch

type Description =
    | Added of string list
    | Changed of string list
    | Deprecated of string list
    | Removed of string list
    | Fixed of string list
    | Security of string list
type ChangesetId = Id of string

type RawChangeset = ChangesetId * string
type RawChangesets = RawChangeset list
type InvalidVersionFormat = InvalidVersionFormat of string

type TargetProject = TargetProject of string

type AffectedProject = {
      Project: TargetProject
      Impact: SemVerImpact
}


type ChangesetContent = {
      AffectedProjects: AffectedProject list
      Descriptions: Description list
}
type Changeset = {
    Id: ChangesetId
    Content: ChangesetContent
}
type ProjectChange =
    { ChangesetId: ChangesetId
      Project: Project
      Impact: SemVerImpact
      Descriptions: Description list
      DependencyGraph: Project list }

    