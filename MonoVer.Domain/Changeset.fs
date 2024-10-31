module MonoVer.Domain.Changeset

open System.Text
open MonoVer.Domain.Types
open FParsec

let private pImpact =
    choice
        [ pstringCI "major" >>% SemVerImpact.Major
          pstringCI "minor" >>% SemVerImpact.Minor
          pstringCI "patch" >>% SemVerImpact.Patch ]

let private pHorizontalLine = pstring "---" .>> many (pchar '-') .>> spaces

let private pSectionHeading name =
    spaces >>. pstring ("# " + name) .>> spaces

// Parser for a project name (quoted string)
let private projectName =
    between (pchar '"') (pchar '"') (manyChars (noneOf "\"")) .>> spaces

// Parser for a single line in the format: "ProjectName": status
let private pAffectedProject =
    projectName .>>. (pchar ':' >>. spaces >>. pImpact)
    |>> (fun (name, impact) ->
        { Project = TargetProject name
          Impact = impact })

// Parser for multiple lines of project impact
let private pAffectedProjects = many (pAffectedProject .>> spaces)

// Parser for the entire document including the dashes
let private pAffectedProjectsSection =
    between pHorizontalLine pHorizontalLine pAffectedProjects

// Parser for description lines (e.g., "Added some new features")
let private pLine = many1Satisfy (fun c -> c <> '\n') .>> newline
let private pHeaderTitle = choice [
    pstring "Added"
    pstring "Changed"
    pstring "Deprecated"
    pstring "Removed"
    pstring "Fixed"
    pstring "Security"
]
// Parser for description header (e.g., "# Added")
let private pHeader = pchar '#' >>. spaces >>. pHeaderTitle .>> newline

// Parser for a section of descriptions (e.g., "# Added\nDescription")
let private pDescriptionSection =
    pipe2 pHeader (manyTill pLine (lookAhead (pHeader <|> (eof >>% "")))) (fun header lines ->
        match header with
        | "Added" -> Added(lines)
        | "Changed" -> Changed(lines)
        | "Deprecated" -> Deprecated(lines)
        | "Removed" -> Removed(lines)
        | "Fixed" -> Fixed(lines)
        | "Security" -> Security(lines)
        | _ -> failwith $"Unknown description header: '{header}'"
        )

// Parser for all sections of descriptions
let private pDescriptions = many1 pDescriptionSection

// Parser for the entire document
let private pChangeset =
    pAffectedProjectsSection .>>. pDescriptions
    |>> (fun (projects, descriptions) ->
        { AffectedProjects = projects
          Descriptions = descriptions })

// Parser for all sections of descriptions
let Parse markdown : Result<ChangesetContent, string> =
    match run pChangeset markdown with
    | Success(changeset, _, _) -> Result.Ok changeset
    | Failure(errorMessage, _, _) -> Result.Error errorMessage

let ParseRaw ((id, content): RawChangeset) : Result<Changeset, PublishError> =
    content
    |> Parse
    |> Result.mapError (fun e -> FailedToParseChangeset(id, e))
    |> Result.map (fun parsed -> { Id = id; Content = parsed })
   
let serializeAffectedProject ({Project = (TargetProject project); Impact = impact}:AffectedProject) (sb:StringBuilder) =
    sb.AppendLine($"\"{project}\": {SemVerImpact.Serialize impact}")
// Serialize an AffectedProject list to the specified format
let serializeAffectedProjects  (projects: AffectedProject list)(sb:StringBuilder) =
    sb.AppendLine("---")
    |> ( List.fold
        (fun sbf ap -> sbf >> serializeAffectedProject ap)
        id<StringBuilder>
        projects)
    |>_.AppendLine("---")

let appendLine (content: string ) (sb: StringBuilder) = sb.AppendLine(content)
// Serialize Descriptions to grouped sections
let serializeSection (title:string, items:string list) =
        match items with
        | [] -> id
        | _ -> List.fold
                (fun sbf item -> (sbf >> (appendLine item)))
                (appendLine $"# {title}")
                items
let serializeDescriptions  (unorderedDescriptions: Description list) =

    // Group descriptions by type and serialize each group
    let descriptions = unorderedDescriptions |> Descriptions.merge
    [("Added", descriptions.Added)
     ("Changed", descriptions.Changed)
     ("Deprecated", descriptions.Deprecated)
     ("Fixed", descriptions.Fixed)
     ("Removed", descriptions.Removed)
     ("Security", descriptions.Security)
     ]
    |> List.fold
        (fun sbf section -> sbf >> serializeSection section)
        id<StringBuilder>

// Main serialization function for ChangesetContent
let Serialize (changeset: ChangesetContent) =
   StringBuilder()
   |> serializeAffectedProjects changeset.AffectedProjects
   |> serializeDescriptions changeset.Descriptions
   |> _.ToString()
