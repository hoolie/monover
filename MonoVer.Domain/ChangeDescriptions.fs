module MonoVer.Domain.ChangeDescriptions

open FSharpPlus
open MonoVer.Domain.Types

let Empty = { Major = []; Minor = [];Patch = [] }
let private collectChanges (changes: ProjectChange list) =
   changes |>> _.Description >>= Option.toList
let Create (changes:ProjectChange list):ChangeDescriptions =
    
    let merge (seed:ChangeDescriptions): (SemVerImpact* ProjectChange list -> ChangeDescriptions) =
        function
        | SemVerImpact.Major, c -> { seed with Major = collectChanges c }
        | SemVerImpact.Minor, c -> { seed with Minor = collectChanges c }
        | SemVerImpact.Patch, c -> { seed with Patch = collectChanges c }
        
    changes
    |> List.groupBy _.Impact
    |> List.fold merge Empty
