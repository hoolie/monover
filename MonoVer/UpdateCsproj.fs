module MonoVer.UpdateCsproj

open FSharpPlus
open MonoVer.Domain.Types
open MonoVer.Domain
open MonoVer.MsProjects

open Microsoft.Build.Evaluation
type UpdateVersionError = FailedToUpdateVersionInFile of VersionIncreased
let private setVersion  (version: Version) (project: Project)=
   project.SetProperty("VersionPrefix", Version.ToString version) |> ignore
   project
   
let ApplyChanges (solution: MsSolution) (results: VersionIncreased ) =
    let (Csproj csprojPath) = results.Project
    let changedProject =
            solution.TryFind csprojPath
            |>> setVersion results.Version
    match changedProject with
    | Some proj -> Ok (proj.Save())
    | None -> Error (FailedToUpdateVersionInFile results)
    