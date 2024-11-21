[<NUnit.Framework.TestFixture>]
module MonoVer.Test.PublishTests

open MonoVer.Domain
open FsUnit
open NUnit.Framework

// Mock data
let mockFileInfo path = ProjectId(path)

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



let mockChangeset id content : RawChangeset = ((ChangesetId id), content)


let mockAffectedProject project impact = { Project = project; Impact = impact }

let mockTargetProject name = ProjectId name



let NewChangelogEntry (project:Project)(version:string) =
    NewChangelogEntry {
        Version = Version.FromString version
        Project = project.Csproj
        Changes = ChangeDescriptions.Empty
    }
let Major desc (event: PublishEvent) =
    match event with
    | NewChangelogEntry evt -> PublishEvent.NewChangelogEntry { evt with Changes.Major = evt.Changes.Minor @ [(ChangeDescription desc)]}
    | _ -> failwith $"Setup failed: expected a NewChangelogEntry but got {event}"
let Minor desc (event: PublishEvent):PublishEvent =
    match event with
    | NewChangelogEntry evt -> PublishEvent.NewChangelogEntry { evt with Changes.Minor = evt.Changes.Minor @ [(ChangeDescription desc)]}
    | _ -> failwith $"Setup failed: expected a NewChangelogEntry but got {event}"

let Patch desc (event: PublishEvent):PublishEvent =
    match event with
    | NewChangelogEntry evt -> PublishEvent.NewChangelogEntry { evt with Changes.Patch = evt.Changes.Patch @ [(ChangeDescription desc)]}
    | _ -> failwith $"Setup failed: expected a NewChangelogEntry but got {event}"


let VersionIncreased (project:Project) (version:string) =
    VersionIncreased
      { Project = project.Csproj
        Version = Version.FromString version }
let ChangesetApplied ((id,_): RawChangeset) =
    ChangesetApplied id
// Unit tests

[<Test>]
let ``No changesets results in no publish results`` () =
    let projects = [ mockProject "TestProject" (mockVersion 1u 0u 0u) [] ]
    let changesets = []
    let result = Changesets.Publish projects changesets
    let value = shouldBeOk result
    value |> should be Empty

[<Test>]
let ``Single changeset results in publish result`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset
            "1"
            """---
"TestProject": minor
---
New feature"""

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let value = shouldBeOk result

    value
    |> should
        equivalent
        [
          NewChangelogEntry project "1.1.0" |> Minor "New feature";
          VersionIncreased project "1.1.0";
          ChangesetApplied changeset
        ]



[<Test>]
let ``Multiple changesets are applied correctly`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset1 =
        mockChangeset
            "1"
            """---
"TestProject": minor
---
New feature"""

    let changeset2 =
        mockChangeset
            "2"
            """---
"TestProject": minor
---
Bug fix"""

    let projects = [ project ]
    let changesets = [ changeset1; changeset2 ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result

    events
    |> should
        equivalent
        [ NewChangelogEntry project "1.1.0" |> Minor "New feature"|> Minor "Bug fix"
          VersionIncreased project "1.1.0"
          ChangesetApplied changeset1
          ChangesetApplied changeset2
        ]

[<Test>]
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
    value |> should equivalent [
        ChangesetApplied changeset
    ]



[<Test>]
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
Feature in ProjectA"""

    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result
    events  |> should equivalent [
        
         NewChangelogEntry projectA "1.1.0"|> Minor "Feature in ProjectA";
         NewChangelogEntry projectB "1.0.1"|> Patch "updated dependency ProjectA";
         NewChangelogEntry projectC "1.0.1"|> Patch "updated dependency ProjectB";
         VersionIncreased projectB "1.0.1"
         VersionIncreased projectC "1.0.1"
         VersionIncreased projectA "1.1.0"
         ChangesetApplied changeset
    ]

[<Test>]
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
Feature in ProjectA
Fix in ProjectB"""

    let changesets = [ changeset ]
    let result = Changesets.Publish projects changesets
    let events = shouldBeOk result

    events
    |> should
        equivalent [
        (NewChangelogEntry projectA "1.1.0" |> Minor
"""Feature in ProjectA
Fix in ProjectB"""
            )
        VersionIncreased projectA "1.1.0"
        
        (NewChangelogEntry projectB "1.0.1" |> Patch
"""Feature in ProjectA
Fix in ProjectB""")
        VersionIncreased projectB "1.0.1"
        ChangesetApplied changeset
        ]

[<Test>]
[<Ignore("not yet implemented")>]
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
    shouldBeError result |> ignore
   
