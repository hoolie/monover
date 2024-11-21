namespace MonoVer.Domain

type Version =
    { Major: uint
      Minor: uint
      Patch: uint }

type InvalidVersionFormat = InvalidVersionFormat of string
module Version =

    open System.Text.RegularExpressions
    open FSharpPlus

    let private Create major minor patch : Version =
        { Major = major
          Minor = minor
          Patch = patch }

    let private versionRegexPattern =
        @"\s*(\d+)\.(\d+)\.(\d+)(?:\.\d+)?(?:-[^<\s]+)?\s*"

    let private MatchVersion (content: string) : Result<GroupCollection, InvalidVersionFormat> =
        let RegexMatch = Regex.Match(content, versionRegexPattern)

        if RegexMatch.Success then
            Result.Ok RegexMatch.Groups
        else
            Result.Error(InvalidVersionFormat content)

    let TryFromString (raw: string) : Result<Version, InvalidVersionFormat> =
        monad {
            let! groups = MatchVersion raw
            let major = System.UInt32.Parse(groups[1].Value)
            let minor = System.UInt32.Parse(groups[2].Value)
            let patch = System.UInt32.Parse(groups[3].Value)
            return Create major minor patch
        }

    let private ErrorToException =
        (function
        | InvalidVersionFormat e -> System.Exception e)

    let FromString (raw: string) : Version =
        match (TryFromString raw) with
        | Ok t -> t
        | Error e -> raise (ErrorToException e)

    let ToString ({Major=major; Minor=minor; Patch=patch}: Version) =
        $"{major}.{minor}.{patch}"
