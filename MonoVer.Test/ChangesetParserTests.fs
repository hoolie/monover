module MonoVer.Test.ChangesetParserTests

open FsUnit
open MonoVer.Domain.Types
open MonoVer.Domain
open NUnit.Framework

type ChangesetParserTests() =

    let createExpectedChangeset() =
        {
          AffectedProjects = [ { Project = TargetProject "ProjectA"; Impact = SemVerImpact.Major }
                               { Project = TargetProject "ProjectB"; Impact = SemVerImpact.Minor } ]
          Description = ChangesetDescription
"""Added some new features
Fixed some bugs
"""
                             }

    [<Test>]
    member _.``Parse method should return Ok with valid changeset when input is correct``() =
        let markdown = """---
"ProjectA": major
"ProjectB": minor
---
Added some new features
Fixed some bugs
"""
        let expected = createExpectedChangeset()
        match Changeset.Parse markdown with
        | Ok changeset -> changeset |> should equal expected
        | Error errorMsg -> failwith $"Expected Ok but got Error: {errorMsg}"

    [<Test>]
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
        match Changeset.Parse invalidMarkdown with
        | Ok _ -> failwith "Expected Error but got Ok"
        | Error _ -> ()

    [<Test>]
    member _.``Parse method should return Error when affected projects section is missing``() =
        let markdownWithoutProjects = """# Added
        Added some new features
        # Fixed
        Fixed some bugs
        """
        match Changeset.Parse markdownWithoutProjects with
        | Ok _ -> failwith "Expected Error but got Ok"
        | Error _ -> ()
        
    [<Test>]
    member _.``Parse method should return Ok when descriptions section is missing``() =
        let markdownWithoutDescriptions = """---
        "ProjectA": major
        "ProjectB": minor
        ---
        """
        match Changeset.Parse markdownWithoutDescriptions with
        | Error _ -> failwith "Expected Ok but got Error"
        | Ok _ -> ()

    [<Test>]
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
            {
              AffectedProjects = [ { Project = TargetProject "ProjectA"; Impact = SemVerImpact.Major }
                                   { Project = TargetProject "ProjectB"; Impact = SemVerImpact.Minor } ]
              Description = ChangesetDescription
"""Added some new features
Fixed some bugs
Changed some functionality"""
            }
        match Changeset.Parse markdown with
        | Ok changeset -> changeset.AffectedProjects |> should equal expected.AffectedProjects
        | Error errorMsg -> failwith $"Expected Ok but got Error: {errorMsg}"
