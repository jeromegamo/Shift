namespace Shift

open Shift.Common

module CommandParser =

    open System.Text.RegularExpressions

    type RawCommand = string

    type ParsedCommand =
        | Initialize of name: MigrationRepositoryName
        | CreateMigration
        | UpdateDatabase
        | RemoveMigration

    type CommandError = string

    let initPattern = 
        Regex("^init$", RegexOptions.Compiled)

    let (|ParseInit|_|) rawCmd = 
        if initPattern.IsMatch rawCmd then Some() else None

    let parseCommand : MigrationRepositoryName -> RawCommand -> Result<ParsedCommand, CommandError> =
        fun migrationRepoName rawCmd -> 
        match rawCmd.Trim() with
        | ParseInit -> Ok (Initialize migrationRepoName)
        | _ -> Error "Invalid command"
