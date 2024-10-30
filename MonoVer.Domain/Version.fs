module MonoVer.Domain.Version

open System
open System.Text.RegularExpressions
open MonoVer.Domain.Types

open FSharpPlus
let private Create major minor patch =
    { Major = major
      Minor = minor
      Patch = patch }
let private versionRegexPattern = @"\s*(\d+)\.(\d+)\.(\d+)(?:\.\d+)?(?:-[^<\s]+)?\s*"

let private MatchVersion (content: string) : Result<GroupCollection, InvalidVersionFormat> =
    let RegexMatch = Regex.Match(content, versionRegexPattern)

    if RegexMatch.Success then
        Result.Ok RegexMatch.Groups
    else
        Result.Error(InvalidVersionFormat content)

let TryFromString (raw: string) : Result<Version, InvalidVersionFormat> =
    monad {
        let! groups = MatchVersion raw
        let major = UInt32.Parse(groups[1].Value)
        let minor = UInt32.Parse(groups[2].Value)
        let patch = UInt32.Parse(groups[3].Value)
        return Create major minor patch
    }

let private ErrorToException =
    (function
    | InvalidVersionFormat e -> Exception e)

let FromString (raw: string) : Version =
    match (TryFromString raw) with
    | Ok t -> t
    | Error e -> raise (ErrorToException e)
    
    
let ToString version =
    $"{version.Major}.{version.Minor}.{version.Patch}"
