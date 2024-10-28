module MonoVer.Cli

open MonoVer.PublishCommand

type ApplicationError =
        | MissingCommand of unit
        | UnknownCommand of string
        | PublishDomainErrors of PublishDomainErrors
        
