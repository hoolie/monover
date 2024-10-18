module MonoVer.Version

open System
open System.Text.RegularExpressions
open MonoVer.ResultBuilder

type VersionParsingError = InvalidVersionFormat of string

type Version =
    { Major: uint
      Minor: uint
      Patch: uint }

let AsString version =
    $"{version.Major}.{version.Minor}.{version.Patch}"

let Create major minor patch =
    { Major = major
      Minor = minor
      Patch = patch }

let versionRegexPattern = @"\s*(\d+)\.(\d+)\.(\d+)(?:\.\d+)?(?:-[^<\s]+)?\s*"

let MatchVersion (content: string) : Result<GroupCollection, VersionParsingError> =
    let RegexMatch = Regex.Match(content, versionRegexPattern)

    if RegexMatch.Success then
        Result.Ok RegexMatch.Groups
    else
        Result.Error(InvalidVersionFormat content)

let TryFromString (raw: string) : Result<Version, VersionParsingError> =
    result {
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
