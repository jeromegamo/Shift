module Shift.UnitTests.InitializationTests

open System
open Xunit
open Swensen.Unquote
open Shift

let Ok' v : Result<ParsedCommand, ParsingError> = Ok v
let Error' v : Result<ParsedCommand, ParsingError> = Error v

[<Theory>]
[<InlineData("init  ")>]
[<InlineData("  init")>]
let ``Command should be an initialization command`` (rawCommand:string) =
    let expected = Ok' (InitCommand)
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("init CreatePlayer")>]
[<InlineData("Init")>]
let ``Command should be invalid`` (rawCommand:string) =
    let expected = Error' "Invalid command"
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>