module Shift.UnitTests.InitializationTests

open System
open Xunit
open Swensen.Unquote
open Shift
open Shift.CommandParser

let Ok' v : Result<ParsedCommand, CommandError> = Ok v
let Error' v : Result<ParsedCommand, CommandError> = Error v

let migrationRepositoryName = "ShiftMigrations"

[<Theory>]
[<InlineData("init  ")>]
[<InlineData("  init")>]
let ``Command should be an initialization command`` (rawCommand:string) =
    let expected = Ok' (Initialize "ShiftMigrations")
    let actual = CommandParser.parseCommand migrationRepositoryName rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("init CreatePlayer")>]
[<InlineData("Init")>]
let ``Command should be invalid`` (rawCommand:string) =
    let expected = Error' "Invalid command"
    let actual = CommandParser.parseCommand migrationRepositoryName rawCommand
    test <@ actual = expected @>