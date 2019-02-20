
module Shift.UnitTests.UpdateCommandParserTests

open System
open Xunit
open Swensen.Unquote
open Shift
    
let Ok' v : Result<ParsedCommand, ParsingError> = Ok v
let Error' v : Result<ParsedCommand, ParsingError> = Error v

[<Theory>]
[<InlineData("update CreatePlayer")>]
[<InlineData(" update CreatePlayer ")>]
[<InlineData("update      CreatePlayer")>]
let ``Should return a parsed command of UpdateDatabase with some name`` (rawCommand:string) =
    let expected = Ok' (UpdateCommand (Some "CreatePlayer"))
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("update")>]
[<InlineData(" update ")>]
let ``Should return a parsed command of UpdateDatabase with no name`` (rawCommand:string) =
    let expected = Ok' (UpdateCommand None)
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>
