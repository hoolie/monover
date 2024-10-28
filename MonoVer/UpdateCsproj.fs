module MonoVer.UpdateCsproj

open Microsoft.Build.Evaluation
open MonoVer.Publish
open MonoVer.Version

let removeVersionProperty (project: Project) =
    match project.GetProperty("Version") with
    | null -> false
    | property  -> project.RemoveProperty property
    
    
let private setVersion (project: Project) (version: Version) =
   removeVersionProperty project |> ignore 
   project.SetProperty("VersionPrefix", AsString version) |> ignore
   project
   
let ApplyChanges (results: PublishResult) =
    let proj = setVersion (Project results.Project.Csproj.FullName) results.NextVersion
    proj.Save()
    