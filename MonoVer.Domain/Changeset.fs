namespace MonoVer.Domain

type ChangesetDescription =
    | ChangesetDescription of string
    | Empty
type ChangesetId = ChangesetId of string

type RawChangeset = ChangesetId * string
type UnvalidatedAffectedProject = {
      Project: string
      Impact: SemVerImpact
}
type PublishError = FailedToParseChangeset of (ChangesetId * string)
                    |ProjectNotFound of string
type UnvalidatedChangeset = {
    Id: ChangesetId
    AffectedProjects: UnvalidatedAffectedProject list
    Description: ChangesetDescription
}

type AffectedProject = {
      Project: ProjectId
      Impact: SemVerImpact
}
type ValidChangeset = {
    Id: ChangesetId
    AffectedProjects: AffectedProject list
    Description: ChangesetDescription
}
type ParseChangeset = RawChangeset -> UnvalidatedChangeset

type ValidateChangeset = UnvalidatedChangeset -> Result<ValidChangeset,PublishError>
module Changeset = 

    open System.Text
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
    let private pAffectedProject:Parser<UnvalidatedAffectedProject,unit> =
        projectName .>>. (pchar ':' >>. spaces >>. pImpact)
        |>> (fun (name, impact) ->
            { Project = name
              Impact = impact })

    // Parser for multiple lines of project impact
    let private pAffectedProjects = many (pAffectedProject .>> spaces)

    // Parser for the entire document including the dashes
    let private pAffectedProjectsSection =
        between pHorizontalLine pHorizontalLine pAffectedProjects

    // Parser for the entire document
    let private pChangeset id : Parser<UnvalidatedChangeset, unit>=
        pAffectedProjectsSection .>>. manyChars anyChar
        |>> (fun (projects, descriptions) ->
            { Id=id
              AffectedProjects = projects
              Description = ChangesetDescription.ChangesetDescription descriptions })

    // Parser for all sections of descriptions
    let Parse id markdown : Result<UnvalidatedChangeset, string> =
        match run (pChangeset id) markdown with
        | Success(changeset, _, _) -> Result.Ok changeset
        | Failure(errorMessage, _, _) -> Result.Error errorMessage

    let private validateAffectedProject
        (projectIds:ProjectId list)
        (affectedProject:UnvalidatedAffectedProject):Result<AffectedProject,PublishError> =
        let projectId = (affectedProject.Project|> ProjectId)
        match Seq.contains projectId projectIds with
        | true -> Result.Ok {Impact = affectedProject.Impact; Project= projectId}
        | false -> Result.Error (ProjectNotFound affectedProject.Project)
        
    let Validate (projectIds) :ValidateChangeset = function
        x -> x.AffectedProjects
             |> Seq.map (validateAffectedProject projectIds)
             |> FSharpPlus.Operators.sequence
             |> Result.map (fun affectedProjects
                             -> {
                 Id = x.Id
                 Description = x.Description
                 AffectedProjects = Seq.toList affectedProjects  
             })
        
    let ParseRaw
        (validProjectIds:ProjectId list) // dependency
        ((id, content): RawChangeset)
            : Result<ValidChangeset, PublishError> =
        content
        |> Parse id
        |> Result.mapError (fun e -> FailedToParseChangeset(id, e))
        |> Result.bind  (Validate validProjectIds)

    let appendLine (content: string) (sb: StringBuilder) = sb.AppendLine(content)
    let serializeAffectedProject
        ({ Project = (ProjectId project)
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
    let Serialize (changeset: ValidChangeset) =
        StringBuilder()
        |> serializeAffectedProjects changeset.AffectedProjects
        |> serializeDescriptions changeset.Description
        |> _.ToString()
