namespace MonoVer.Domain

type VersionPrefix =
    { Major: uint
      Minor: uint
      Patch: uint }

type VersionSuffix = VersionSuffix of string

type Version =
    | PreviewVersion of VersionPrefix * VersionSuffix
    | ReleaseVersion of VersionPrefix
            

type InvalidVersionFormat = InvalidVersionFormat of string

module VersionPrefix =

    open System.Text.RegularExpressions
    open FSharpPlus

    let private Create major minor patch : VersionPrefix =
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

    let TryFromString (raw: string) : Result<VersionPrefix, InvalidVersionFormat> =
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

    let FromString (raw: string) : VersionPrefix =
        match (TryFromString raw) with
        | Ok t -> t
        | Error e -> raise (ErrorToException e)

    let ToString
        ({ Major = major
           Minor = minor
           Patch = patch }: VersionPrefix)
        =
        $"{major}.{minor}.{patch}"
module Version =
    let Create prefix (VersionSuffix suffix): Version =
        match (System.String.IsNullOrWhiteSpace suffix) with
        | false -> PreviewVersion (prefix, VersionSuffix suffix)
        | true -> ReleaseVersion prefix
    let toString = function
        | ReleaseVersion x -> VersionPrefix.ToString x
        | PreviewVersion (prefix,suffix) -> $"{VersionPrefix.ToString prefix}-{suffix}"
    