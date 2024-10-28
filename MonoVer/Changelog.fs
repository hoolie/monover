module MonoVer.ChangelogEntry

open System
open System.IO
open System.Text.RegularExpressions
open MonoVer.Changeset
open MonoVer.Version

type Changelog  = {
    Path: FileInfo
    Content: string
}
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
    
let VersionSectionHeaderRegex = Regex("^##\s*\[\d*\.\d*\.\d*\]\s*-\s*\d*-\d*-\d*\s*$")
let formatChanges (changes:Descriptions) =
    [
     ("Added", changes.Added);
     ("Changed", changes.Changed);
     ("Deprecated", changes.Deprecated);
     ("Fixed", changes.Fixed);
     ("Removed", changes.Removed);
     ("Security", changes.Security)
     ]
    |> List.filter(fun (_,e) -> not (Seq.isEmpty e))
    |> List.collect (fun (section, content) -> $"### {section}" :: content )
let format (entry:ChangelogVersionEntry) =
     $"## [{entry.Version |> AsString}] - {entry.Date:``yyyy-MM-dd``}"
        :: (formatChanges entry.Changes)
    
let AddEntryToChangelog (entry: ChangelogVersionEntry) (rawChangelog: Changelog) =
    let formattedEntry = format entry
    let lines = rawChangelog.Content.Split("\n")|> List.ofArray
    let firstVersionLine =
        List.indexed lines
        |> Seq.filter (fun (_,x) -> VersionSectionHeaderRegex.IsMatch(x))
        |> Seq.map fst
        |> Seq.tryHead
    let newChangelog =
        match firstVersionLine with
        | Some lineNumber -> (List.take lineNumber lines)  @ formattedEntry @ (List.skip lineNumber lines)
        | None -> lines @ formattedEntry 
    {Content = String.Join( Environment.NewLine, newChangelog); Path = rawChangelog.Path }