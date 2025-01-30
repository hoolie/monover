module MonoVer.Test.ChangesetParserTests


open Xunit
open FsUnit
open MonoVer.ChangesetParser
open MonoVer.Changeset

type ChangesetParserTests() =

    let createExpectedChangeset() =
        { Id = ChangesetId.Id "45"
          AffectedProjects = [ { Project = Csproj "ProjectA"; Impact = SemVerImpact.Major }
                               { Project = Csproj "ProjectB"; Impact = SemVerImpact.Minor } ]
          Descriptions = [ Added [ "Added some new features" ]
                           Fixed [ "Fixed some bugs" ] ] }

    [<Fact>]
    member _.``Parse method should return Ok with valid changeset when input is correct``() =
        let markdown = """---
"ProjectA": major
"ProjectB": minor
---
# Added
Added some new features
# Fixed
Fixed some bugs
"""
        let expected = createExpectedChangeset()
        match Parse markdown with
        | Ok changeset -> Assert.Equal(expected, changeset)
        | Error errorMsg -> Assert.True(false, $"Expected Ok but got Error: {errorMsg}")

    [<Fact>]
    member _.``Parse method should return Error when input is incorrect``() =
        let invalidMarkdown = """---
        "ProjectA": major
        "ProjectB": unknown
        ---
        # Added
        Added some new features
        # Fixed
        Fixed some bugs
        """
        match Parse invalidMarkdown with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error _ -> Assert.True(true)

    [<Fact>]
    member _.``Parse method should return Error when affected projects section is missing``() =
        let markdownWithoutProjects = """# Added
        Added some new features
        # Fixed
        Fixed some bugs
        """
        match Parse markdownWithoutProjects with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error _ -> Assert.True(true)

    [<Fact>]
    member _.``Parse method should return Error when descriptions section is missing``() =
        let markdownWithoutDescriptions = """---
        "ProjectA": major
        "ProjectB": minor
        ---
        """
        match Parse markdownWithoutDescriptions with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error _ -> Assert.True(true)

    [<Fact>]
    member _.``Parse method should return Ok with valid changeset when input contains more sections``() =
        let markdown = """---
"ProjectA": major
"ProjectB": minor
---
# Added
Added some new features
# Fixed
Fixed some bugs
# Changed
Changed some functionality
"""
        let expected = 
            { Id = ChangesetId.Id "45"
              AffectedProjects = [ { Project = Csproj "ProjectA"; Impact = SemVerImpact.Major }
                                   { Project = Csproj "ProjectB"; Impact = SemVerImpact.Minor } ]
              Descriptions = [ Added [ "Added some new features" ]
                               Fixed [ "Fixed some bugs" ]
                               Changed [ "Changed some functionality" ] ] }
        match Parse markdown with
        | Ok changeset -> changeset.AffectedProjects |> should equal expected.AffectedProjects
        | Error errorMsg -> Assert.True(false, $"Expected Ok but got Error: {errorMsg}")
