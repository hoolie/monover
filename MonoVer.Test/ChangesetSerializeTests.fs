module MonoVer.Test.ChangesetSerializeTests

open MonoVer.Domain
open FsUnit
open NUnit.Framework
// Define test data structures to simulate the serialization function's inputs
let createTestChangeset() =
    {
        AffectedProjects = [
            { Project = ProjectId "ProjectA"; Impact = SemVerImpact.Major }
            { Project = ProjectId "ProjectB"; Impact = SemVerImpact.Minor }
        ]
        Description = ChangesetDescription.ChangesetDescription
                          """Added a new feature
Fixed a critical bug
Modified configuration settings"""
    }

[<Test>]
let ``Serialize should produce expected output for given changeset``() =
    let expectedOutput = 
        """---
"ProjectA": major
"ProjectB": minor
---
Added a new feature
Fixed a critical bug
Modified configuration settings
"""

    let changeset = createTestChangeset()
    let result = Changeset.Serialize changeset
    result |> should equal expectedOutput

[<Test>]
let ``Serialize should handle empty affected projects and descriptions``() =
    let expectedOutput = 
        """---
---
"""

    let emptyChangeset = { AffectedProjects = []; Description = ChangesetDescription.Empty }
    let result = Changeset.Serialize emptyChangeset
    result |> should equal expectedOutput

[<Test>]
let ``Serialize should handle only affected projects with no descriptions``() =
    let expectedOutput = 
        """---
"ProjectA": patch
---
"""

    let changesetWithOnlyProjects = {
        AffectedProjects = [
            { Project = ProjectId "ProjectA"; Impact = SemVerImpact.Patch }
        ]
        Description = ChangesetDescription.Empty
    }
    let result = Changeset.Serialize changesetWithOnlyProjects
    result |> should equal expectedOutput

[<Test>]
let ``Serialize should handle only descriptions with no affected projects``() =
    let expectedOutput = 
        """---
---
Removed deprecated method
Patched security vulnerability
"""

    let changesetWithOnlyDescriptions = {
        AffectedProjects = []
        Description = ChangesetDescription """Removed deprecated method
Patched security vulnerability"""
        
    }
    let result = Changeset.Serialize changesetWithOnlyDescriptions
    result |> should equal expectedOutput