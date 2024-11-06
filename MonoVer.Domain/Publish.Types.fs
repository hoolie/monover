namespace MonoVer.Domain.Types

type ChangeDescriptions =
    { Major: ChangeDescription list
      Minor: ChangeDescription list
      Patch: ChangeDescription list }

type NewChangelogEntry =
    { Project: Csproj
      Changes: ChangeDescriptions
      Version: Version }

type VersionIncreased = { Project: Csproj; Version: Version }

type PublishError = FailedToParseChangeset of (ChangesetId * string)

type PublishEvent =
    | NewChangelogEntry of NewChangelogEntry
    | VersionIncreased of VersionIncreased
    | ChangesetApplied of ChangesetId

type MergedChangesets = Result<PublishEvent list, PublishError>
type ProcessChangesets = RawChangesets -> MergedChangesets
