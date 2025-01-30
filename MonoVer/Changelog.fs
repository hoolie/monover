module MonoVer.ChangelogEntry

open System
open MonoVer.Changeset
open MonoVer.Version

type Descriptions =
    | Added of string list
    | Changed of string list
    | Deprecated of string list
    | Removed of string list
    | Fixed of string list
    | Security of string list

type ChangelogVersionEntry =
    { Version: Version
      Date: DateOnly
      Changes: Descriptions list }

type Changelog =
    { Preamble: string
      Versions: ChangelogVersionEntry list }


let AddEntry descriptions text =
    match descriptions with
    | Added x -> Added (text::x)
    | Changed x -> Changed (text::x)
    | Deprecated x -> Deprecated (text::x)
    | Removed x -> Removed (text::x)
    | Fixed x -> Fixed (text::x)
    | Security x -> Security (text::x)
  
let TryAddChangesetDescription (changelogDescriptions: Descriptions) (changesetDescription:Changeset.Description) =
    match (changelogDescriptions, changesetDescription) with
    | Added x, Changeset.Added y -> Some (Added (y@x))
    | Changed x, Changeset.Changed y -> Some (Changed (y@x))
    | Deprecated x, Changeset.Deprecated y -> Some (Deprecated (y@x))
    | Removed x, Changeset.Removed y -> Some (Removed (y@x))
    | Fixed x, Changeset.Fixed y -> Some (Fixed (y@x))
    | Security x, Changeset.Security y -> Some (Security (y@x))
    | _,_ -> None
    
// let AddChangesetDescription (changelogDescriptions: Descriptions list) (changesetDescription: Description) =
//     changelogDescriptions|> List.map (fun changelogDescription match (_,changesetDescription) with
//                                       | (Added s, Added t) -> )
    