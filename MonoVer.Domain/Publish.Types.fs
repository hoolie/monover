namespace MonoVer.Domain.Types

open System.IO

type NewChangelogEntry = { Project: FileInfo; Changes: Descriptions; Version: Version  }
type VersionIncreased = { Project: FileInfo; Version: Version  }

type PublishError =  FailedToParseChangeset of (ChangesetId * string)
   
type PublishEvent =
    | NewChangelogEntry of NewChangelogEntry
    | VersionIncreased of VersionIncreased
    | ChangesetApplied of ChangesetId
    
type MergedChangesets = Result<PublishEvent list, PublishError>
type ProcessChangesets =  RawChangesets -> MergedChangesets
