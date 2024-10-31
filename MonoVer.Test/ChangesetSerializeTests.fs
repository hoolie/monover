module MonoVer.Test.ChangesetSerializeTests

open Xunit
open FsUnit.Xunit
open MonoVer.Domain.Types
open MonoVer.Domain

// Define test data structures to simulate the serialization function's inputs
let createTestChangeset() =
    {
        AffectedProjects = [
            { Project = TargetProject "ProjectA"; Impact = SemVerImpact.Major }
            { Project = TargetProject "ProjectB"; Impact = SemVerImpact.Minor }
        ]
        Descriptions = [
            Added ["Added a new feature"]
            Fixed ["Fixed a critical bug"]
            Changed ["Modified configuration settings"]
        ]
    }

[<Fact>]
let ``Serialize should produce expected output for given changeset``() =
    let expectedOutput = 
        """---
"ProjectA": major
"ProjectB": minor
---
# Added
Added a new feature
# Changed
Modified configuration settings
# Fixed
Fixed a critical bug
"""

    let changeset = createTestChangeset()
    let result = Changeset.Serialize changeset
    result |> should equal expectedOutput

[<Fact>]
let ``Serialize should handle empty affected projects and descriptions``() =
    let expectedOutput = 
        """---
---
"""

    let emptyChangeset = { AffectedProjects = []; Descriptions = [] }
    let result = Changeset.Serialize emptyChangeset
    result |> should equal expectedOutput

[<Fact>]
let ``Serialize should handle only affected projects with no descriptions``() =
    let expectedOutput = 
        """---
"ProjectA": patch
---
"""

    let changesetWithOnlyProjects = {
        AffectedProjects = [
            { Project = TargetProject "ProjectA"; Impact = SemVerImpact.Patch }
        ]
        Descriptions = []
    }
    let result = Changeset.Serialize changesetWithOnlyProjects
    result |> should equal expectedOutput

[<Fact>]
let ``Serialize should handle only descriptions with no affected projects``() =
    let expectedOutput = 
        """---
---
# Removed
Removed deprecated method
# Security
Patched security vulnerability
"""

    let changesetWithOnlyDescriptions = {
        AffectedProjects = []
        Descriptions = [
            Removed ["Removed deprecated method"]
            Security ["Patched security vulnerability"]
        ]
    }
    let result = Changeset.Serialize changesetWithOnlyDescriptions
    result |> should equal expectedOutput