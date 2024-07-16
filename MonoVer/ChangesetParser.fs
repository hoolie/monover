module MonoVer.ChangesetParser

open FParsec
open MonoVer.Changeset

let pImpact =
    choice
        [ pstringCI "major" >>% SemVerImpact.Major
          pstringCI "minor" >>% SemVerImpact.Minor
          pstringCI "patch" >>% SemVerImpact.Patch ]

let pHorizontalLine = pstring "---" .>> many (pchar '-') .>> spaces

let pSectionHeading name =
    spaces >>. pstring ("# " + name) .>> spaces

// Parser for a project name (quoted string)
let projectName =
    between (pchar '"') (pchar '"') (manyChars (noneOf "\"")) .>> spaces

// Parser for a single line in the format: "ProjectName": status
let pAffectedProject =
    projectName .>>. (pchar ':' >>. spaces >>. pImpact)
    |>> (fun (name, impact) ->
        { Project = Csproj name
          Impact = impact })

// Parser for multiple lines of project impact
let pAffectedProjects = many (pAffectedProject .>> spaces)

// Parser for the entire document including the dashes
let pAffectedProjectsSection =
    between pHorizontalLine pHorizontalLine pAffectedProjects

// Parser for description lines (e.g., "Added some new features")
let pLine = many1Satisfy (fun c -> c <> '\n') .>> newline
// Parser for description header (e.g., "# Added")
let pHeader = pchar '#' >>. spaces >>. pLine

// Parser for a section of descriptions (e.g., "# Added\nDescription")
let pDescriptionSection =
    pipe2 pHeader (manyTill pLine (lookAhead (pHeader <|> (eof >>% "")))) (fun header lines ->
        match header with
        | "Added" -> Added(lines)
        | "Changed" -> Changed(lines)
        | "Deprecated" -> Deprecated(lines)
        | "Removed" -> Removed(lines)
        | "Fixed" -> Fixed(lines)
        | "Security" -> Security(lines)
        | _ -> failwith "Unknown description header")

// Parser for all sections of descriptions
let pDescriptions = many1 pDescriptionSection

// Parser for the entire document
let pChangeset =
    pAffectedProjectsSection .>>. pDescriptions
    |>> (fun (projects, descriptions) ->
        {  
          AffectedProjects = projects
          Descriptions = descriptions })

// Parser for all sections of descriptions
let Parse markdown : Result<ChangesetContent, string> =
    match run pChangeset markdown with
    | Success(changeset, _, _) -> Result.Ok changeset
    | Failure(errorMessage, _, _) -> Result.Error errorMessage
