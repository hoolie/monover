module MonoVer.Test.PublishTests

open System.IO
open MonoVer.Domain.Types
open MonoVer.Domain
open Xunit
open FsUnit

// Mock data
let mockFileInfo path = FileInfo(path)

let shouldBeOk result =
    match result with
    | Ok value -> value
    | Error e -> failwith $"Expected Ok, but got Error: {e}"

let shouldBeError result =
    match result with
    | Ok value -> failwith $"Expected Error, but got Ok: {value}"
    | Error e -> e

let mockProject name version dependencies : Project =
    { Csproj = mockFileInfo name
      CurrentVersion = version
      Dependencies = dependencies }

let mockVersion major minor patch : Version =
    { Major = major
      Minor = minor
      Patch = patch }

let mockDescriptions added changed deprecated removed fix security : Descriptions =
    { Added = added
      Changed = changed
      Deprecated = deprecated
      Removed = removed
      Fixed = fix
      Security = security }

let mockChangeset id content : RawChangeset = ((Id id), content)


let mockAffectedProject project impact = { Project = project; Impact = impact }

let mockTargetProject name = Csproj name

let mockDescription added changed deprecated removed fix security =
    [ Added added
      Changed changed
      Deprecated deprecated
      Removed removed
      Fixed fix
      Security security ]

// Unit tests
[<Fact>]
let ``No changesets results in no publish results`` () =
    let projects = [ mockProject "TestProject" (mockVersion 1u 0u 0u) [] ]
    let changesets = []
    let result = Changesets.Publish projects changesets
    let value = shouldBeOk result
    value |> should be Empty

[<Fact>]
let ``Single changeset results in publish result`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset
            "1"
            """---
"TestProject": minor
---
# Added
New feature
"""

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let value = shouldBeOk result

    value
    |> should
        equal
        [ NewChangelogEntry
              { Project = Csproj project.Csproj.FullName
                Version = Version.FromString("1.1.0")
                Changes = [ Added [ "New feature" ] ] |> Descriptions.merge }
          VersionIncreased
              { Project = Csproj project.Csproj.FullName
                Version = Version.FromString("1.1.0") }
          ChangesetApplied(Id "1") ]



[<Fact>]
let ``Multiple changesets are applied correctly`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset1 =
        mockChangeset
            "1"
            """---
"TestProject": minor
---
# Added
New feature
"""

    let changeset2 =
        mockChangeset
            "2"
            """---
"TestProject": minor
---
# Fixed
Bug fix
"""

    let projects = [ project ]
    let changesets = [ changeset1; changeset2 ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result

    events
    |> should
        equal
        [ NewChangelogEntry
              { Project = Csproj project.Csproj.FullName
                Version = Version.FromString("1.1.0")
                Changes = [ Added [ "New feature" ]; Fixed [ "Bug fix" ] ] |> Descriptions.merge }
          VersionIncreased
              { Project = Csproj project.Csproj.FullName
                Version = Version.FromString("1.1.0") }
          ChangesetApplied(Id "1")
          ChangesetApplied(Id "2") ]

[<Fact>]
let ``Changeset with no affected projects does not affect publish result`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset
            "1"
            """---
---
# Added
New feature
"""

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let value = shouldBeOk result
    value |> should equal [ ChangesetApplied(Id "1") ]



[<Fact>]
let ``Transitive dependencies are correctly updated`` () =
    let projectA = mockProject "ProjectA" (mockVersion 1u 0u 0u) []
    let projectB = mockProject "ProjectB" (mockVersion 1u 0u 0u) [ projectA ]
    let projectC = mockProject "ProjectC" (mockVersion 1u 0u 0u) [ projectB ]
    let projects = [ projectA; projectB; projectC ]

    let changeset =
        mockChangeset
            "1"
            """---
"ProjectA": minor
---
# Added
Feature in ProjectA
"""

    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result

    events
    |> should
        contain
        (NewChangelogEntry
            { Project = Csproj projectA.Csproj.FullName
              Version = Version.FromString("1.1.0")
              Changes = [ Added [ "Feature in ProjectA" ] ] |> Descriptions.merge })

    events
    |> should
        contain
        (VersionIncreased
            { Project = Csproj projectB.Csproj.FullName
              Version = Version.FromString("1.1.0") })

    events
    |> should
        contain
        (VersionIncreased
            { Project = Csproj projectC.Csproj.FullName
              Version = Version.FromString("1.1.0") })

[<Fact>]
let ``Transient dependency is overwritten when project is defined in changeset`` () =
    let projectA = mockProject "ProjectA" (mockVersion 1u 0u 0u) []
    let projectB = mockProject "ProjectB" (mockVersion 1u 0u 0u) [ projectA ]

    let projects = [ projectA; projectB ]

    let changeset =
        mockChangeset
            "1"
            """---
"ProjectA": minor
"ProjectB": patch
---
# Added
Feature in ProjectA
# Fixed
Fix in ProjectB
"""

    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result

    events
    |> should
        contain
        (NewChangelogEntry
            { Project = Csproj projectA.Csproj.FullName
              Version = Version.FromString("1.1.0")
              Changes =
                [ Added [ "Feature in ProjectA" ]; Fixed [ "Fix in ProjectB" ] ]
                |> Descriptions.merge })

    events
    |> should
        contain
        (VersionIncreased
            { Project = Csproj projectA.Csproj.FullName
              Version = Version.FromString("1.1.0") })

    events
    |> should
        contain
        (NewChangelogEntry
            { Project = Csproj projectB.Csproj.FullName
              Version = Version.FromString("1.0.1")
              Changes =
                [ Added [ "Feature in ProjectA" ]; Fixed [ "Fix in ProjectB" ] ]
                |> Descriptions.merge })

    events
    |> should
        contain
        (VersionIncreased
            { Project = Csproj projectB.Csproj.FullName
              Version = Version.FromString("1.0.1") })

[<Fact(Skip = "not yet implemented")>]
let ``Changeset with invalid projects should yield an error`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset
            "1"
            """---
    "NonExistentProject": minor
    ---
    # Added
    New feature
    """

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    shouldBeError result
   
