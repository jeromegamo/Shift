namespace Shift

open Shift.Common

module CommandParser =

    open System.Text.RegularExpressions

    type RawCommand = string

    type ParsedCommand =
        | Initialize of name: MigrationRepositoryName
        | AddMigration of name: MigrationEntryName
        | UpdateDatabase
        | RemoveMigration

    type CommandError = string

    let initPattern = 
        Regex("^init$", RegexOptions.Compiled)
    let addPattern = 
        Regex("^add[ ]*(?<name>[a-zA-Z0-9]+)$", RegexOptions.Compiled)

    let (|ParseInit|_|) rawCmd = 
        if initPattern.IsMatch rawCmd then Some() else None

    let (|ParseAdd|_|) rawCmd =
        let m = addPattern.Match rawCmd
        if m.Success 
        then (m.Groups.["name"].Value).Trim() |> Some
        else None

    let parseCommand : MigrationRepositoryName -> RawCommand -> Result<ParsedCommand, CommandError> =
        fun migrationRepoName rawCmd -> 
        match rawCmd.Trim() with
        | ParseInit -> Ok (Initialize migrationRepoName)
        | ParseAdd migrationEntryName -> Ok (AddMigration migrationEntryName)
        | _ -> Error "Invalid command"
