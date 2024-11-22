module MonoVer.Test.ChangesetParserTests

open FsUnit
open MonoVer.Domain
open NUnit.Framework

type ChangesetParserTests() =

    let expectedChangeset: UnvalidatedChangeset =
        {
          
          Id = ChangesetId "1"
          AffectedProjects = [ { Project =  "ProjectA"; Impact = SemVerImpact.Major }
                               { Project = "ProjectB"; Impact = SemVerImpact.Minor } ]
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
        match Changeset.Parse (ChangesetId "1") markdown with
        | Ok changeset -> changeset |> should equal expectedChangeset
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
        match Changeset.Parse (ChangesetId "1") invalidMarkdown with
        | Ok _ -> failwith "Expected Error but got Ok"
        | Error _ -> ()

    [<Test>]
    member _.``Parse method should return Error when affected projects section is missing``() =
        let markdownWithoutProjects = """# Added
        Added some new features
        # Fixed
        Fixed some bugs
        """
        match Changeset.Parse (ChangesetId "1") markdownWithoutProjects with
        | Ok _ -> failwith "Expected Error but got Ok"
        | Error _ -> ()
        
    [<Test>]
    member _.``Parse method should return Ok when descriptions section is missing``() =
        let markdownWithoutDescriptions = """---
        "ProjectA": major
        "ProjectB": minor
        ---
        """
        match Changeset.Parse (ChangesetId "1") markdownWithoutDescriptions with
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
        let expected: UnvalidatedChangeset = 
            {
              Id = ChangesetId "1"
              AffectedProjects = [ { Project = "ProjectA"; Impact = SemVerImpact.Major }
                                   { Project = "ProjectB"; Impact = SemVerImpact.Minor } ]
              Description = ChangesetDescription
"""Added some new features
Fixed some bugs
Changed some functionality"""
            }
        match Changeset.Parse (ChangesetId "1") markdown with
        | Ok changeset -> changeset.AffectedProjects |> should equivalent expected.AffectedProjects
        | Error errorMsg -> failwith $"Expected Ok but got Error: {errorMsg}"
