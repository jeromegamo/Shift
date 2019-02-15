
module Shift.UnitTests.UpdateCommandParserTests

open System
open Xunit
open Swensen.Unquote
open Shift
open Shift.CommandParser
    
let Ok' v : Result<ParsedCommand, CommandError> = Ok v
let Error' v : Result<ParsedCommand, CommandError> = Error v

let migrationRepositoryName = "ShiftMigrations"

[<Theory>]
[<InlineData("update CreatePlayer")>]
[<InlineData(" update CreatePlayer ")>]
[<InlineData("update      CreatePlayer")>]
let ``Should return a parsed command of UpdateDatabase with some name`` (rawCommand:string) =
    let expected = Ok' (UpdateDatabase (Some "CreatePlayer"))
    let actual = CommandParser.parseCommand migrationRepositoryName rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("update")>]
[<InlineData(" update ")>]
let ``Should return a parsed command of UpdateDatabase with no name`` (rawCommand:string) =
    let expected = Ok' (UpdateDatabase None)
    let actual = CommandParser.parseCommand migrationRepositoryName rawCommand
    test <@ actual = expected @>
