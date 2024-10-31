namespace MonoVer


module Projects = 

    open MonoVer.MsProjects
    open System.Collections.Generic
    open System.IO
    open MonoVer.Domain.Types
    open MonoVer.Domain

    type private SlnProject = string * Version
    type private SlnProjectWithReferences = SlnProject * string seq


    let private parseLoadedProject (p: MsProject): SlnProjectWithReferences =
            let version = Version.FromString(p.GetPropertyValue("Version"))
            let slnProj = SlnProject((p.FullPath), version)
            let projectReferences = p.GetItems("ProjectReference")
            let deps =
                projectReferences
                |> Seq.map (_.EvaluatedInclude)
                |> Seq.map (fun rel -> Path.Join(p.DirectoryPath, rel))
                |> Seq.map FileInfo
                |> Seq.map _.FullName

            SlnProjectWithReferences(slnProj, deps)

    // Function to create a graph of Projects from a sequence of SlnProjectWithReferences
    let private createGraph (projectsWithRefs: SlnProjectWithReferences seq) =
        // Step 1: Create a map from FileInfo.FullName to SlnProject for quick lookup
        let projectMap =
            projectsWithRefs
            |> Seq.map fst // Extract the SlnProject
            |> Seq.map (fun (fileInfo, version) -> (fileInfo, (fileInfo, version))) // Map file paths to SlnProjects
            |> Map.ofSeq

        // Step 2: Create an initial unresolved node map with empty dependencies
        let resolvedProjects = Dictionary<string, Project>()

        // Recursive function to resolve dependencies and create fully connected Project nodes
        let rec resolveProject (project: SlnProject) : Project =
            let csproj, version = project

            match resolvedProjects.TryGetValue(csproj) with
            | true, proj -> proj // Return already resolved Project
            | _ ->
                // Find the SlnProjectWithReferences associated with the project
                let projectWithRefs = projectsWithRefs |> Seq.tryFind (fun (p, _) -> fst p = csproj)

                match projectWithRefs with
                | Some(_, references) ->
                    // Recursively resolve dependencies
                    let resolvedDependencies =
                        references
                        |> Seq.choose (fun refFileInfo -> Map.tryFind refFileInfo projectMap) // Map FileInfo to SlnProject
                        |> Seq.map resolveProject // Recursively resolve dependencies
                        |> Seq.toList

                    // Create the Project node
                    let resolvedProject =
                        { Csproj = FileInfo csproj
                          CurrentVersion = version
                          Dependencies = resolvedDependencies }

                    // Memoize the resolved project
                    resolvedProjects.Add(csproj, resolvedProject)
                    resolvedProject
                | None ->
                    // Handle case where the project is not found, which shouldn't happen
                    let proj =
                        { Csproj = FileInfo csproj
                          CurrentVersion = version
                          Dependencies = [] }

                    resolvedProjects.Add(csproj, proj)
                    proj

        // Step 3: Resolve all projects starting from each project in the input
        projectsWithRefs |> Seq.map fst |> Seq.map resolveProject |> Seq.toList // Convert to list or keep as sequence

    let FromSolution (solution: MsSolution) = 
        solution
        |> Map.values
        |> Seq.map parseLoadedProject
        |> createGraph
