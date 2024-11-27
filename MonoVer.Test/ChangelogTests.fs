module MonoVer.Test.ChangelogTests

open System
open System.IO
open MonoVer
open MonoVer.Domain
open FsUnit
open NUnit.Framework

let CreateChangelog content = {Content = content; Path = FileInfo "none"}
type ChangelogTests() =

    [<Test>]
    member _.``should insert the first entry if no versions exist``() =
        let markdown = """All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
"""
        let entry:ChangelogVersionEntry = {
            Version = ReleaseVersion (VersionPrefix.FromString("1.0.0"))
            Date = DateOnly.Parse("2024-10-15")
            Changes = { ChangeDescriptions.Empty with Major = [ChangeDescription "First release" ]}
        }
        let result = Changelog.AddEntry (CreateChangelog markdown) entry
        result.Content |> should equal """All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-10-15
### Major
First release"""

    [<Test>]
    member _.``should correctly append a new version entry above an existing one``() =
        let markdown = """## [1.0.0] - 2023-01-01
- Initial release
"""
        let entry:ChangelogVersionEntry = {
            Version = ReleaseVersion (VersionPrefix.FromString("1.1.0"))
            Date = DateOnly.Parse("2024-10-15")
            Changes = { ChangeDescriptions.Empty with Minor = [ChangeDescription "Added Feature X\n" ]}
        }
        let result = Changelog.AddEntry (CreateChangelog markdown) entry
        result.Content |> should equal """## [1.1.0] - 2024-10-15
### Minor
Added Feature X

## [1.0.0] - 2023-01-01
- Initial release
"""

    [<Test>]
    member _.``should handle multiple change types correctly``() =
        let markdown = """## [1.0.0] - 2023-01-01

### Added
- Initial release
"""
        let entry:ChangelogVersionEntry = {
            Version = ReleaseVersion (VersionPrefix.FromString("1.1.0"))
            Date = DateOnly.Parse("2024-10-15")
            Changes = { ChangeDescriptions.Empty
                        with
                            Minor = [ChangeDescription "- New feature"]
                            Patch = [ChangeDescription "- Deprecated API" ;ChangeDescription"- Bug fix"]
                       }
        }
        let result = Changelog.AddEntry (CreateChangelog markdown) entry 
        result.Content |> should equal """## [1.1.0] - 2024-10-15
### Minor
- New feature
### Patch
- Deprecated API
- Bug fix
## [1.0.0] - 2023-01-01

### Added
- Initial release
"""
    [<Test>]
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
            Version = ReleaseVersion (VersionPrefix.FromString("1.1.1"))
            Date = DateOnly.Parse("2024-10-15")
            Changes = {ChangeDescriptions.Empty
                       with
                        Patch = [ChangeDescription "- Fixed some Important Feature"]}
            
        } 
        let result = Changelog.AddEntry (CreateChangelog markdown) entry 
        result.Content |> should contain "## [1.1.1] - 2024-10-15"
        