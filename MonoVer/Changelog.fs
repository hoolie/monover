namespace MonoVer

open System.IO
open System
open FSharpPlus
open MonoVer.Domain
type Changelog  = {
    Path: FileInfo
    Content: string
}

type ChangelogVersionEntry =
        { Version: Version
          Date: DateOnly
          Changes: ChangeDescriptions }
module Changelog = 

    open System.Text.RegularExpressions
    open MonoVer.Domain

    let VersionSectionHeaderRegex = Regex("^##\s*\[\d*\.\d*\.\d*\]\s*-\s*\d*-\d*-\d*\s*$")
    let formatChanges (changes:ChangeDescriptions) =
        [
         ("Major", changes.Major);
         ("Minor", changes.Minor);
         ("Patch", changes.Patch);
         ]
        |> List.filter(fun (_,e) -> not (Seq.isEmpty e))
        |> List.collect (fun (section, content) -> $"### {section}" :: (content|>> fun(ChangeDescription desc) -> desc ))
    let format (entry:ChangelogVersionEntry) =
         $"## [{Version.ToString entry.Version}] - {entry.Date:``yyyy-MM-dd``}"
            :: (formatChanges entry.Changes)
        
    let AddEntry (rawChangelog: Changelog) (entry: ChangelogVersionEntry)  =
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