namespace MonoVer.Domain

type ParseImpactError = FailedToParseImpact of string
module SemVerImpact =

    open FSharpPlus
    open MonoVer.Domain.Types
    
    let Parse rawImpact =
        match String.toLower rawImpact with
        | "major" -> Ok SemVerImpact.Major
        | "minor" -> Ok SemVerImpact.Minor
        | "patch" -> Ok SemVerImpact.Patch
        | _ -> Error(FailedToParseImpact rawImpact)
        
    let Serialize (impact: SemVerImpact) =
        match impact with
        | SemVerImpact.Major -> "major"
        | SemVerImpact.Minor -> "minor"
        | SemVerImpact.Patch -> "patch"
