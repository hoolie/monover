module MonoVer.ChangelogEntry

open System
open MonoVer.Changeset
open MonoVer.Version

type Descriptions = {
    Added: string list
    Changed: string list
    Deprecated: string list
    Removed: string list
    Fixed: string list
    Security: string list
}

type ChangelogVersionEntry =
    { Version: Version
      Date: DateOnly
      Changes: Descriptions }

type Changelog =
    { Preamble: string
      Versions: ChangelogVersionEntry list }

let emptyDescriptions = {
    Added = []
    Changed = []
    Deprecated = []
    Removed = []
    Fixed = []
    Security = []
}
let AddEntry descriptions text =
    match descriptions with
    | Added x -> Added (text::x)
    | Changed x -> Changed (text::x)
    | Deprecated x -> Deprecated (text::x)
    | Removed x -> Removed (text::x)
    | Fixed x -> Fixed (text::x)
    | Security x -> Security (text::x)
  
let mergeDescriptions descriptions =
    let addToCategory  categoryList description =
        match description with
        | Added lst -> { categoryList with Added = categoryList.Added @ lst }
        | Changed lst -> { categoryList with Changed = categoryList.Changed @ lst }
        | Deprecated lst -> { categoryList with Deprecated = categoryList.Deprecated @ lst }
        | Removed lst -> { categoryList with Removed = categoryList.Removed @ lst }
        | Fixed lst -> { categoryList with Fixed = categoryList.Fixed @ lst }
        | Security lst -> { categoryList with Security = categoryList.Security @ lst }

    List.fold addToCategory emptyDescriptions descriptions
    
    