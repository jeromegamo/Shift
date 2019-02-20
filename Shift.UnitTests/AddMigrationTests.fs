module Shift.UnitTests.AddMigrationTests

open System
open Xunit
open Swensen.Unquote
open Shift
open Shift.CommandParser
    
let Ok' v : Result<ParsedCommand, ParsingError> = Ok v
let Error' v : Result<ParsedCommand, ParsingError> = Error v

[<Theory>]
[<InlineData("add CreatePlayer")>]
[<InlineData(" add CreatePlayer ")>]
[<InlineData("add      CreatePlayer")>]
let ``Should return a parsed command of AddMigration`` (rawCommand:string) =
    let expected = Ok' (AddCommand "CreatePlayer")
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("add")>]
[<InlineData(" add ")>]
let ``Should be an invalid command`` (rawCommand:string) =
    let expected = Error' "Invalid command"
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>