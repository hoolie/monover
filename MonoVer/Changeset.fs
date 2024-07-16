module MonoVer.Changeset

open System.IO

type SemVerImpact =
    | Major = 0
    | Minor = 1
    | Patch = 2

type TargetProject =
    | Csproj of string

    member this.Path =
        match this with
        | Csproj p -> p

type AffectedProject =
    { Project: TargetProject
      Impact: SemVerImpact }

type Description =
    | Added of string list
    | Changed of string list
    | Deprecated of string list
    | Removed of string list
    | Fixed of string list
    | Security of string list
type ChangesetId = Id of string
type ChangesetContent =
    {
      AffectedProjects: AffectedProject list
      Descriptions: Description list }
    
type Changeset = {
    Id: ChangesetId
    Content: ChangesetContent
}
