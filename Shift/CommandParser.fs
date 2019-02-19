namespace Shift

open System
open Shift.Common

module CommandParser =

    open System.Text.RegularExpressions

    type RawCommand = string

    type ParsedCommand =
        | Initialize of name: MigrationRepositoryName
        | AddMigration of name: MigrationEntryName
        | UpdateDatabase of name : MigrationEntryName option
        | RemoveMigration

    type CommandError = string

    let initPattern = 
        Regex("^init$", RegexOptions.Compiled)
    let addPattern = 
        Regex("^add[ ]*(?<name>[a-zA-Z0-9]+)$", RegexOptions.Compiled)
    let updatePattern = 
        Regex("^update[ ]*(?<name>[a-zA-Z0-9]*)$", RegexOptions.Compiled)
    let private removePattern = 
        Regex("^remove$", RegexOptions.Compiled)

    let (|ParseInit|_|) rawCmd = 
        if initPattern.IsMatch rawCmd then Some() else None

    let (|ParseAdd|_|) rawCmd =
        let m = addPattern.Match rawCmd
        if m.Success 
        then (m.Groups.["name"].Value).Trim() |> Some
        else None

    let private (|ParseUpdate|_|) rawCmd = 
        let m = updatePattern.Match rawCmd
        if m.Success then 
            let name = m.Groups.["name"].Value
            if String.IsNullOrWhiteSpace name then Some(None)
            else Some(Some(name))
        else None

    let (|ParseRemove|_|) raw = if removePattern.IsMatch raw then Some() else None

    let parseCommand : MigrationRepositoryName -> RawCommand -> Result<ParsedCommand, CommandError> =
        fun migrationRepoName rawCmd -> 
        match rawCmd.Trim() with
        | ParseInit -> Ok (Initialize migrationRepoName)
        | ParseAdd migrationEntryName -> Ok (AddMigration migrationEntryName)
        | ParseUpdate migrationEntryName -> Ok (UpdateDatabase migrationEntryName)
        | ParseRemove -> Ok(RemoveMigration)
        | _ -> Error "Invalid command"
