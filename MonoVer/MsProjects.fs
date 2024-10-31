namespace MonoVer

type MsProjectsError = 
    | SolutionFileNotFoundInWorkdir of string
    | MultipleSolutionFilesFoundInWorkdir of string seq
module MsProjects = 

    open System.IO
    open Microsoft.Build.Construction
    open Microsoft.Build.Evaluation

    type MsProject = Project
    type MsSolution = Map<string, MsProject>


    let Load (pathToSolution: string) : MsSolution =
        SolutionFile.Parse pathToSolution
        |>_.ProjectsInOrder
        |> Seq.filter (fun p -> p.ProjectType = SolutionProjectType.KnownToBeMSBuildFormat)
        |> Seq.map (fun p -> (p.AbsolutePath, Project(p.AbsolutePath)))
        |> Map
        
    let TryLoadFrom(workdir: string) =
         match (DirectoryInfo(workdir).GetFiles("*.sln") |> Array.map _.FullName) with
            | [||] -> Error(SolutionFileNotFoundInWorkdir workdir)
            | [| x |] -> Ok(Load x)
            | files -> Error(MultipleSolutionFilesFoundInWorkdir files)
