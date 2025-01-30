module MonoVer.Version

type Version =
    { Major: uint
      Minor: uint
      Patch: uint }
let AsString  version = $"{version.Major}.{version.Minor}.{version.Patch}"
