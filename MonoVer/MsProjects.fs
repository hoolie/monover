namespace MonoVer

open MonoVer.Domain

type MsProjectsError = 
    | SolutionFileNotFoundInWorkdir of string
    | MultipleSolutionFilesFoundInWorkdir of string seq
module MsProjects = 

    open System.IO
    open Microsoft.Build.Construction
    open Microsoft.Build.Evaluation

    type MsProject = Project
    type MsSolution = Map<ProjectId, MsProject>


    let Load (pathToSolution: string) : MsSolution =
        SolutionFile.Parse pathToSolution
        |>_.ProjectsInOrder
        |> Seq.filter (fun p -> p.ProjectType = SolutionProjectType.KnownToBeMSBuildFormat)
        |> Seq.map (fun p ->  Project(p.AbsolutePath))
        |> Seq.map (fun p -> ((ProjectId (p.GetPropertyValue("MsBuildProjectName"))), p))
        |> Map
        
    let TryLoadFrom(workdir: string) =
         match (DirectoryInfo(workdir).GetFiles("*.sln") |> Array.map _.FullName) with
            | [||] -> Error(SolutionFileNotFoundInWorkdir workdir)
            | [| x |] -> Ok(Load x)
            | files -> Error(MultipleSolutionFilesFoundInWorkdir files)
