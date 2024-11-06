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

// Parser for the entire document
let private pChangeset =
    pAffectedProjectsSection .>>. manyChars anyChar
    |>> (fun (projects, descriptions) ->
        { AffectedProjects = projects
          Description = ChangesetDescription.ChangesetDescription descriptions })

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

let appendLine (content: string) (sb: StringBuilder) = sb.AppendLine(content)
let serializeAffectedProject
    ({ Project = (TargetProject project)
       Impact = impact }: AffectedProject)
    = appendLine $"\"{project}\": {SemVerImpact.Serialize impact}"
// Serialize an AffectedProject list to the specified format
let serializeAffectedProjects (projects: AffectedProject list)  =
    appendLine "---"
    >> (List.fold (fun sbf ap -> sbf >> serializeAffectedProject ap) id<StringBuilder> projects)
    >> appendLine "---"

// Serialize Descriptions to grouped sections
let serializeDescriptions (unorderedDescriptions: ChangesetDescription) =
     match unorderedDescriptions with
      | ChangesetDescription x -> appendLine x
      | Empty -> id<StringBuilder>



// Main serialization function for ChangesetContent
let Serialize (changeset: ChangesetContent) =
    StringBuilder()
    |> serializeAffectedProjects changeset.AffectedProjects
    |> serializeDescriptions changeset.Description
    |> _.ToString()
