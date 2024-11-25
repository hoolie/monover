module MonoVer.Cli

type ApplicationError =
        | MissingCommand of unit
        | NotImplemented of string
        | CommandError of (int*string)
        
