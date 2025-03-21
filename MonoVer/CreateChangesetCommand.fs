namespace MonoVer

open System
open System.IO
open FSharpPlus
open CommandLine
open MonoVer.Cli
open MonoVer.Domain

[<Verb("new", HelpText = "Add a new changeset.")>]
type NewChangesetOption =
    {
      [<Option('w', "Workdir", Default = ".")>]
      Workdir: string
      [<Option('n', "name", Default = "")>]
      Name: string
      [<Option('i', "impact", Required = true, HelpText ="The impact this change has on the affected project" )>]
      Impact: string
      [<Option('p', "projects", Required = true, HelpText = "The project(s) that are affected")>]
      Projects: string seq
      [<Option('m', "message", Required = true, HelpText = "The project(s) that are affected")>]
      Message: string
       }
   
type CreateChangesetError =
    | MsProjectsError of MsProjectsError
    | ParseImpactError of ParseImpactError
    | ProjectNotFound of string
module CreateChangesetCommand =
    
    let private tryFindProjectInSolution (solution: MsProjects.MsSolution) (projectName: string) =
            
            Map.keys solution
            |> Seq.tryFind (fun key -> key = (ProjectId projectName))
            |> function
                | Some project -> Ok project
                | None -> Result.Error (ProjectNotFound projectName)
    let Run ({
        Name = rawName
        Impact = rawImpact
        Projects = projects
        Message = message
        Workdir = workdir
    }: NewChangesetOption): Result<unit,ApplicationError> =
        let changesetName =
            match String.IsNullOrWhiteSpace rawName with
            | true -> Guid.NewGuid().ToString()
            | false -> String.trimEnd ".md" rawName
        monad {
            let! impact = SemVerImpact.Parse rawImpact
                            |> Result.mapError ParseImpactError
            let! solution = MsProjects.TryLoadFrom workdir
                            |> Result.mapError MsProjectsError
            let! validatedProjects =
                    projects
                    |> Seq.toList
                    |>> (tryFindProjectInSolution solution)
                    |> sequence
                    
            let affectedProjects =
                        validatedProjects
                        |>> (fun proj-> {Project= proj; Impact = impact})
            let changeset = Changeset.Serialize {
                Id = ChangesetId "todo"
                AffectedProjects = affectedProjects
                Description = ChangesetDescription.ChangesetDescription message
            }
            File.WriteAllText (Path.Join(workdir, ".changesets", changesetName + ".md"), changeset)
            Console.WriteLine($"Created: {changesetName}.md")
            return ()
             
        }
        |> Result.mapError (
            function
            | MsProjectsError (SolutionFileNotFoundInWorkdir x) -> CommandError (-5, $"Could not find any solution file in working directory '{x}'") 
            | MsProjectsError (MultipleSolutionFilesFoundInWorkdir x) -> CommandError (-6, $"""Found multiple solution file in working directory: {x}""")
            | ParseImpactError (FailedToParseImpact rawImpact) -> CommandError (-8, $"Failed to parse impact: {rawImpact}")
            | ProjectNotFound project -> CommandError (-9, $"Project not found: {project}")
            )
        

      
