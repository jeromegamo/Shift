namespace Shift

open System
open Shift.Common

type RawCommand = string
type ParsingError = string
type ParsedCommand =
    | InitCommand
    | AddCommand of name: MigrationEntryName
    | UpdateCommand of name : MigrationEntryName option
    | RemoveCommand

module CommandParser =

    open System.Text.RegularExpressions

    let private initPattern = 
        Regex("^init$", RegexOptions.Compiled)
    let private addPattern = 
        Regex("^add[ ]*(?<name>[a-zA-Z0-9]+)$", RegexOptions.Compiled)
    let private updatePattern = 
        Regex("^update[ ]*(?<name>[a-zA-Z0-9]*)$", RegexOptions.Compiled)
    let private removePattern = 
        Regex("^remove$", RegexOptions.Compiled)

    let private (|InitParser|_|) rawCmd = 
        if initPattern.IsMatch rawCmd then Some() else None

    let private (|AddParser|_|) rawCmd =
        let m = addPattern.Match rawCmd
        if m.Success 
        then (m.Groups.["name"].Value).Trim() |> Some
        else None

    let private (|UpdateParser|_|) rawCmd = 
        let m = updatePattern.Match rawCmd
        if m.Success then 
            let name = m.Groups.["name"].Value
            if String.IsNullOrWhiteSpace name then Some(None)
            else Some(Some(name))
        else None

    let private (|RemoveParser|_|) raw = if removePattern.IsMatch raw then Some() else None

    let parseCommand : RawCommand -> Result<ParsedCommand, ParsingError> =
        fun rawCmd -> 
        match rawCmd.Trim() with
        | InitParser -> Ok (InitCommand)
        | AddParser migrationEntryName -> Ok (AddCommand migrationEntryName)
        | UpdateParser migrationEntryName -> Ok (UpdateCommand migrationEntryName)
        | RemoveParser -> Ok(RemoveCommand)
        | _ -> Error "Invalid command"
