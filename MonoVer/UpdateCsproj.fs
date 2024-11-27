module MonoVer.UpdateCsproj

open FSharpPlus
open MonoVer.Domain
open MonoVer.MsProjects

open Microsoft.Build.Evaluation
type UpdateVersionError = FailedToUpdateVersionInFile of VersionIncreased
let private setVersion  (version: Version) (project: Project)=
   match version with
   | ReleaseVersion prefix ->
            project.SetProperty("VersionPrefix", VersionPrefix.ToString prefix) |> ignore
   | PreviewVersion (prefix, VersionSuffix suffix ) ->
            project.SetProperty("VersionPrefix", VersionPrefix.ToString prefix) |> ignore
            project.SetProperty("VersionSuffix", suffix) |> ignore
   project
   
let ApplyChanges (solution: MsSolution) (event: VersionIncreased ) =
    let changedProject =
            solution.TryFind event.Project
            |>> setVersion event.Version
    match changedProject with
    | Some proj -> Ok (proj.Save())
    | None -> Error (FailedToUpdateVersionInFile event)
    