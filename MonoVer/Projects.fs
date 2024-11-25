namespace MonoVer


module Projects =

    open MonoVer.MsProjects
    open System.Collections.Generic
    open System.IO
    open MonoVer.Domain

    type Reference = string

    type private SlnProjectWithReferences =
        { Version: Version
          ProjectId: ProjectId
          ProjectFile: string
          References: Reference seq }

    let tryGetVersionPrefix (project: MsProject) =
        let versionPrefix = project.GetProperty("VersionPrefix")
        match versionPrefix.IsImported with
        | true -> None
        | false -> Some (Version.FromString versionPrefix.EvaluatedValue)

    let private parseLoadedProject (p: MsProject) : SlnProjectWithReferences seq =
        let projectName = p.GetPropertyValue("MsBuildProjectName")
        let projectId = ProjectId projectName
        let version = tryGetVersionPrefix p
        let projectReferences = p.GetItems("ProjectReference")

        let deps =
            projectReferences
            |> Seq.map (_.EvaluatedInclude)
            |> Seq.map (fun rel -> Path.Join(p.DirectoryPath, rel))
            |> Seq.map FileInfo
            |> Seq.map _.FullName

        version
        |> Option.map (fun version ->
            { Version = version
              ProjectId = projectId
              ProjectFile = p.FullPath
              References = deps })
        |> Option.toList
        |> List.toSeq

    // Function to create a graph of Projects from a sequence of SlnProjectWithReferences
    let private createGraph (projectsWithRefs: SlnProjectWithReferences seq) =
        // Step 1: Create a map from FileInfo.FullName to SlnProject for quick lookup
        let projectMap =
            projectsWithRefs
            |> Seq.map (fun p -> (p.ProjectFile, p)) // Map file paths to SlnProjects
            |> Map.ofSeq

        // Step 2: Create an initial unresolved node map with empty dependencies
        let resolvedProjects = Dictionary<string, Project>()

        // Recursive function to resolve dependencies and create fully connected Project nodes
        let rec resolveProject (project: SlnProjectWithReferences) : Project =
            let { ProjectId = projectId
                  ProjectFile = csproj
                  Version = version } =
                project

            match resolvedProjects.TryGetValue(csproj) with
            | true, proj -> proj // Return already resolved Project
            | _ ->
                // Find the SlnProjectWithReferences associated with the project
                let projectWithRefs =
                    projectsWithRefs |> Seq.tryFind (fun p -> p.ProjectFile = csproj)

                match projectWithRefs with
                | Some proj ->
                    // Recursively resolve dependencies
                    let resolvedDependencies =
                        proj.References
                        |> Seq.choose (fun refFileInfo -> Map.tryFind refFileInfo projectMap) // Map FileInfo to SlnProject
                        |> Seq.map resolveProject // Recursively resolve dependencies
                        |> Seq.toList

                    // Create the Project node
                    let resolvedProject =
                        { Id = projectId
                          CurrentVersion = version
                          Dependencies = resolvedDependencies }

                    // Memoize the resolved project
                    resolvedProjects.Add(csproj, resolvedProject)
                    resolvedProject
                | None ->
                    // Handle case where the project is not found, which shouldn't happen
                    let proj =
                        { Id = projectId // todo: this should be the real project name
                          CurrentVersion = version
                          Dependencies = [] }

                    resolvedProjects.Add(csproj, proj)
                    proj

        // Step 3: Resolve all projects starting from each project in the input
        projectsWithRefs |> Seq.map resolveProject |> Seq.toList // Convert to list or keep as sequence

    let FromSolution (solution: MsSolution) =
        solution |> Map.values |> Seq.collect parseLoadedProject |> createGraph
