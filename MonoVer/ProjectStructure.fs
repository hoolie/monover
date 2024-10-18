module MonoVer.ProjectStructure

open System.IO
open MonoVer.Version

type Project = {
    Csproj: FileInfo
    CurrentVersion: Version
    Dependencies: Project list
}

let Create csproj currentVersion dependencies: Project = {
    Csproj = csproj
    CurrentVersion = currentVersion
    Dependencies = dependencies 
}

