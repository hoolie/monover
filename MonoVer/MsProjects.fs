module MonoVer.MsProjects

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
