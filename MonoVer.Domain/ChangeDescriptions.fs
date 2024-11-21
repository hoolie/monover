namespace MonoVer.Domain

type ChangeDescription  = ChangeDescription of string
type ChangeDescriptions =
    { Major: ChangeDescription list
      Minor: ChangeDescription list
      Patch: ChangeDescription list }
    

type DescriptionWithImpact = 
      {
        Impact: SemVerImpact
        Description: ChangeDescription 
      }

    
module ChangeDescriptions = 

    open FSharpPlus

    let Empty = { Major = []; Minor = [];Patch = [] }
    let private collectDescriptions (changes: DescriptionWithImpact list) =
       changes |>> _.Description 
    let Create (changes:DescriptionWithImpact list):ChangeDescriptions =
        
        let merge (seed:ChangeDescriptions): (SemVerImpact* DescriptionWithImpact list -> ChangeDescriptions) =
            function
            | SemVerImpact.Major, c -> { seed with Major = collectDescriptions c }
            | SemVerImpact.Minor, c -> { seed with Minor = collectDescriptions c }
            | SemVerImpact.Patch, c -> { seed with Patch = collectDescriptions c }
            
        changes
        |> List.groupBy _.Impact
        |> List.fold merge Empty
