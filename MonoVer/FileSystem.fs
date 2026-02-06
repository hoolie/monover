module MonoVer.FileSystem

open System.IO
open FSharpPlus

type FindChangesetRootError = ChangesetRootNotFound of string
let rec findRoot' (currentDir: DirectoryInfo) : DirectoryInfo option =
    let dir = Path.Combine(currentDir.FullName, ".changesets")
    let pathExists = Directory.Exists dir

    match (pathExists, currentDir.Parent) with
    | true, _ -> Some currentDir
    | _, null -> None
    | _, parent -> findRoot' parent
    
let findRoot (workdir: string):  Result< string, FindChangesetRootError> =
    findRoot' (DirectoryInfo(workdir))
    |>> _.FullName
    |> Option.toResultWith (ChangesetRootNotFound workdir)