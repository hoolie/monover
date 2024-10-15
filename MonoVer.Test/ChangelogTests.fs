module MonoVer.Test.ChangelogTests

open System
open MonoVer
open MonoVer.ChangelogEntry
open Xunit
open FsUnit
open MonoVer.Changeset

type ChangelogTests() =

    [<Fact>]
    member _.``should insert the first entry if no versions exist``() =
        let markdown = """All notable changes to this project will be documented in this file.

    The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
    and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
    """
        let entry:ChangelogVersionEntry = {
            Version = Version.FromString("1.0.0")
            Date = DateOnly.Parse("2024-10-15")
            Changes = [Added ["- First release"]] |> mergeDescriptions
        }
        let result = AddEntryToChangelog entry {Content = markdown}
        result.Content |> should contain "## [1.0.0] - 2024-10-15"
        result.Content |> should contain "### Added\n- First release"

    [<Fact>]
    member _.``should correctly append a new version entry above an existing one``() =
        let markdown = """## [1.0.0] - 2023-01-01

    ### Added
    - Initial release
    """
        let entry:ChangelogVersionEntry = {
            Version = Version.FromString("1.1.0")
            Date = DateOnly.Parse("2024-10-15")
            Changes = [Changed ["- Updated feature"]] |> mergeDescriptions
        }
        let result = AddEntryToChangelog entry {Content = markdown}
        result.Content |> should contain "## [1.1.0] - 2024-10-15"
        result.Content |> should contain "### Changed\n- Updated feature"
        result.Content |> should startWith "## [1.1.0] - 2024-10-15"

    [<Fact>]
    member _.``should handle multiple change types correctly``() =
        let markdown = """## [1.0.0] - 2023-01-01

    ### Added
    - Initial release
    """
        let entry:ChangelogVersionEntry = {
            Version = Version.FromString("1.1.0")
            Date = DateOnly.Parse("2024-10-15")
            Changes = [
                Added ["- New feature"]
                Fixed ["- Bug fix"]
                Deprecated ["- Deprecated API"]
            ] |> mergeDescriptions
        }
        let result = AddEntryToChangelog entry {Content = markdown}
        result.Content |> should contain "## [1.1.0] - 2024-10-15"
        result.Content |> should contain "### Added\n- New feature"
        result.Content |> should contain "### Fixed\n- Bug fix"
        result.Content |> should contain "### Deprecated\n- Deprecated API"

    [<Fact>]
    member _.``should insert the entry in above the last entry``() =
        let markdown = """All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2023-03-05

### Added
- Added some more stuff

### Fixed
- Fixed some more stuff

### Changed
- Changed some more stuff

### Removed
- Removed some more stuff

## [1.0.0] - 2023-03-05

### Added
- Added some stuff

### Fixed
- Fixed some stuff

### Changed
- Changed some stuff

### Removed
- Removed some stuff
"""
        let entry:ChangelogVersionEntry = {
            Version = Version.FromString("1.1.1")
            Date = DateOnly.Parse("2024-10-15")
            Changes = [Fixed ["- Some Important Feature"]] |> mergeDescriptions
            
        } 
        let result = AddEntryToChangelog  entry {Content=markdown}
        result.Content |> should contain "## [1.1.1] - 2024-10-15"
        