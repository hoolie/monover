module MonoVer.Cli

type ApplicationError =
        | MissingCommand of unit
        | UnknownCommand of string
        | CommandError of (int*string)
        
