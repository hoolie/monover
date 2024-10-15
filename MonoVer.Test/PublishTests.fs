module MonoVer.Test.PublishTests

open System.IO
open MonoVer.ChangelogEntry
open MonoVer.Changeset
open MonoVer.ProjectStructure
open MonoVer.Publish
open MonoVer.Version
open Xunit

// Mock data
let mockFileInfo path = FileInfo(path)

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

let mockChangeset id affectedProjects descriptions : Changeset =
    { Id = Id id
      Content =
        {
          AffectedProjects = affectedProjects
          Descriptions = descriptions
        }
    }

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
    let result = Publish projects changesets
    Assert.Empty(result)

[<Fact>]
let ``Single changeset results in publish result`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset
            "1"
            [ mockAffectedProject (mockTargetProject "TestProject") SemVerImpact.Minor ]
            (mockDescription [ "New feature" ] [] [] [] [] [])

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Publish projects changesets
    Assert.NotEmpty(result)
    let publishResult = result.[0]
    Assert.Equal(project, publishResult.Project)
    Assert.Equal<string list>([ "New feature" ], publishResult.Changes.Added)
    Assert.Equal((mockVersion 1u 1u 0u), publishResult.NextVersion)

[<Fact>]
let ``Multiple changesets are applied correctly`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset1 =
        mockChangeset
            "1"
            [ mockAffectedProject (mockTargetProject "TestProject") SemVerImpact.Minor ]
            (mockDescription [ "New feature" ] [] [] [] [] [])

    let changeset2 =
        mockChangeset
            "2"
            [ mockAffectedProject (mockTargetProject "TestProject") SemVerImpact.Patch ]
            (mockDescription [] [] [] [] [ "Bug fix" ] [])

    let projects = [ project ]
    let changesets = [ changeset1; changeset2 ]
    let result = Publish projects changesets
    Assert.NotEmpty(result)
    let publishResult = result.[0]
    Assert.Equal(project, publishResult.Project)

    Assert.Equal<string list>(
        (mockDescriptions [ "New feature" ] [] [] [] [ "Bug fix" ] []).Added,
        publishResult.Changes.Added
    )

    Assert.Equal((mockVersion 1u 1u 0u), publishResult.NextVersion)

[<Fact>]
let ``Changeset with no affected projects does not affect publish result`` () =
    let project = mockProject "TestProject" (mockVersion 1u 0u 0u) []

    let changeset =
        mockChangeset "1" [] (mockDescription [ "New feature" ] [] [] [] [] [])

    let projects = [ project ]
    let changesets = [ changeset ]
    let result = Publish projects changesets
    Assert.Empty(result)

[<Fact>]
let ``Transitive dependencies are correctly updated`` () =
    let projectA = mockProject "ProjectA" (mockVersion 1u 0u 0u) []
    let projectB = mockProject "ProjectB" (mockVersion 1u 0u 0u) [ projectA ]
    let projectC = mockProject "ProjectC" (mockVersion 1u 0u 0u) [ projectB ]
    let projects = [ projectA; projectB; projectC ]

    let changeset =
        mockChangeset
            "1"
            [ mockAffectedProject (mockTargetProject "ProjectA") SemVerImpact.Minor ]
            (mockDescription [ "Feature in ProjectA" ] [] [] [] [] [])

    let changesets = [ changeset ]

    let result = Publish projects changesets

    Assert.Equal(3, List.length result)

    let publishResultA =
        result |> List.find (fun r -> r.Project.Csproj.Name = "ProjectA")

    Assert.Equal((mockVersion 1u 1u 0u), publishResultA.NextVersion)

    let publishResultB =
        result |> List.find (fun r -> r.Project.Csproj.Name = "ProjectB")

    Assert.Equal((mockVersion 1u 1u 0u), publishResultB.NextVersion)
    Assert.Contains("updated dependency ProjectA", publishResultB.Changes.Changed)

    let publishResultC =
        result |> List.find (fun r -> r.Project.Csproj.Name = "ProjectC")

    Assert.Equal((mockVersion 1u 1u 0u), publishResultC.NextVersion)
    Assert.Contains("updated dependency ProjectB", publishResultC.Changes.Changed)

[<Fact>]
let ``Transient dependency is overwritten when project is defined in changeset`` () =
    let projectA = mockProject "ProjectA" (mockVersion 1u 0u 0u) []
    let projectB = mockProject "ProjectB" (mockVersion 1u 0u 0u) [ projectA ]

    let projects = [ projectA; projectB ]

    let changeset =
        mockChangeset
            "1"
            [ mockAffectedProject (mockTargetProject "ProjectA") SemVerImpact.Minor
              mockAffectedProject (mockTargetProject "ProjectB") SemVerImpact.Patch ]
            (mockDescription [ "Feature in ProjectA" ] [] [] [] [ "Fix in ProjectB" ] [])

    let changesets = [ changeset ]

    let result = Publish projects changesets

    Assert.Equal(2, List.length result)

    let publishResultA =
        result |> List.find (fun r -> r.Project.Csproj.Name = "ProjectA")

    Assert.Equal((mockVersion 1u 1u 0u), publishResultA.NextVersion)
    Assert.Equal<string list>([ "Feature in ProjectA" ], publishResultA.Changes.Added)

    let publishResultB =
        result |> List.find (fun r -> r.Project.Csproj.Name = "ProjectB")

    Assert.Equal((mockVersion 1u 0u 1u), publishResultB.NextVersion)
    Assert.Equal<string list>([ "Fix in ProjectB" ], publishResultB.Changes.Fixed)
    Assert.Empty(publishResultB.Changes.Changed)
