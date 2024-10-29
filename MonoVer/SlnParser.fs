module MonoVer.SlnParser

open System.Collections.Generic
open System.IO
open Microsoft.Build.Construction
open Microsoft.Build.Evaluation
open MonoVer.ProjectStructure

type SlnProject = FileInfo * Version.Version
type SlnProjectWithReferences = SlnProject * FileInfo seq

type MsProject = Microsoft.Build.Evaluation.Project
type MsSolution = Map<string, MsProject>

let LoadProjects (slnFile: SolutionFile) : MsSolution =
    slnFile.ProjectsInOrder
        |> Seq.filter (fun p -> p.ProjectType = SolutionProjectType.KnownToBeMSBuildFormat)
        |> Seq.map (fun p -> (p.AbsolutePath, Project(p.AbsolutePath)))
        |> Map

let ParseLoadedProject (p: Microsoft.Build.Evaluation.Project): SlnProjectWithReferences =
        let version = Version.FromString(p.GetPropertyValue("Version"))
        let slnProj = SlnProject((FileInfo p.FullPath), version)
        let projectReferences = p.GetItems("ProjectReference")
        let deps =
            projectReferences
            |> Seq.map (_.EvaluatedInclude)
            |> Seq.map (fun rel -> Path.Join(p.DirectoryPath, rel))
            |> Seq.map FileInfo

        SlnProjectWithReferences(slnProj, deps)

// Function to create a graph of Projects from a sequence of SlnProjectWithReferences
let createGraph (projectsWithRefs: SlnProjectWithReferences seq) =
    // Step 1: Create a map from FileInfo.FullName to SlnProject for quick lookup
    let projectMap =
        projectsWithRefs
        |> Seq.map fst // Extract the SlnProject
        |> Seq.map (fun (fileInfo, version) -> (fileInfo.FullName, (fileInfo, version))) // Map file paths to SlnProjects
        |> Map.ofSeq

    // Step 2: Create an initial unresolved node map with empty dependencies
    let resolvedProjects = Dictionary<string, Project>()

    // Recursive function to resolve dependencies and create fully connected Project nodes
    let rec resolveProject (project: SlnProject) : Project =
        let csproj, version = project

        match resolvedProjects.TryGetValue(csproj.FullName) with
        | true, proj -> proj // Return already resolved Project
        | _ ->
            // Find the SlnProjectWithReferences associated with the project
            let projectWithRefs = projectsWithRefs |> Seq.tryFind (fun (p, _) -> fst p = csproj)

            match projectWithRefs with
            | Some(_, references) ->
                // Recursively resolve dependencies
                let resolvedDependencies =
                    references
                    |> Seq.choose (fun refFileInfo -> Map.tryFind refFileInfo.FullName projectMap) // Map FileInfo to SlnProject
                    |> Seq.map resolveProject // Recursively resolve dependencies
                    |> Seq.toList

                // Create the Project node
                let resolvedProject =
                    { Csproj = csproj
                      CurrentVersion = version
                      Dependencies = resolvedDependencies }

                // Memoize the resolved project
                resolvedProjects.Add(csproj.FullName, resolvedProject)
                resolvedProject
            | None ->
                // Handle case where the project is not found, which shouldn't happen
                let proj =
                    { Csproj = csproj
                      CurrentVersion = version
                      Dependencies = [] }

                resolvedProjects.Add(csproj.FullName, proj)
                proj

    // Step 3: Resolve all projects starting from each project in the input
    projectsWithRefs |> Seq.map fst |> Seq.map resolveProject |> Seq.toList // Convert to list or keep as sequence

let getProjects (msProjects: MsProject seq) : Project list =
    msProjects
    |> Seq.map ParseLoadedProject
    |> createGraph
