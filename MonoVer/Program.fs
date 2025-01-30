// For more information see https://aka.ms/fsharp-console-apps

let markdown = """
---
"projectA": patch
"projectB": patch
---
# Added
bla
blub
# Fixed
blub
"""
let result = MonoVer.ChangesetParser.Parse markdown

match result with
| Ok changeset ->
    printfn $"Parsed successfully: %A{changeset}"
| Error errorMsg ->
    printfn $"Parsing failed: %s{errorMsg}"
printfn "hi"