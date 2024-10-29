module MonoVer.UpdateCsproj

open Microsoft.Build.Evaluation
open MonoVer.Publish
open MonoVer.SlnParser
open MonoVer.Version

open FSharpPlus

type UpdateVersionError = FailedToUpdateVersionInFile of PublishResult
let private setVersion  (version: Version) (project: Project)=
   project.SetProperty("VersionPrefix", AsString version) |> ignore
   project
   
let ApplyChanges (solution: MsSolution) (results: PublishResult) =
    let changedProject =
            solution.TryFind results.Project.Csproj.FullName
            |>> setVersion results.NextVersion
    match changedProject with
    | Some proj -> Ok (proj.Save())
    | None -> Error (FailedToUpdateVersionInFile results)
    