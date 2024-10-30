namespace MonoVer.Domain.Types

type NewChangelogEntry = { Project: Csproj; Changes: Descriptions; Version: Version  }
type VersionIncreased = { Project: Csproj; Version: Version  }

type PublishError =  FailedToParseChangeset of (ChangesetId * string)
   
type PublishEvent =
    | NewChangelogEntry of NewChangelogEntry
    | VersionIncreased of VersionIncreased
    | ChangesetApplied of ChangesetId
    
type MergedChangesets = Result<PublishEvent list, PublishError>
type ProcessChangesets =  RawChangesets -> MergedChangesets
