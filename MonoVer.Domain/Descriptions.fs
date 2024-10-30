module MonoVer.Domain.Descriptions

open MonoVer.Domain.Types
let private emptyDescriptions = {
    Added = []
    Changed = []
    Deprecated = []
    Removed = []
    Fixed = []
    Security = []
}

let merge descriptions =
    let addToCategory  categoryList description =
        match description with
        | Added lst -> { categoryList with Added = categoryList.Added @ lst }
        | Changed lst -> { categoryList with Changed = categoryList.Changed @ lst }
        | Deprecated lst -> { categoryList with Deprecated = categoryList.Deprecated @ lst }
        | Removed lst -> { categoryList with Removed = categoryList.Removed @ lst }
        | Fixed lst -> { categoryList with Fixed = categoryList.Fixed @ lst }
        | Security lst -> { categoryList with Security = categoryList.Security @ lst }

    List.fold addToCategory emptyDescriptions descriptions
