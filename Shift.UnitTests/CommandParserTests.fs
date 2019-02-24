module Shift.UnitTests.CommandParserTests

open System
open Xunit
open Swensen.Unquote
open Shift

let Ok' v : Result<ParsedCommand, ParsingError> = Ok v
let Error' v : Result<ParsedCommand, ParsingError> = Error v

[<Theory>]
[<InlineData("init  ")>]
[<InlineData("  init")>]
let ``Should be an init command`` (rawCommand:string) =
    let expected = Ok' (InitCommand)
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("init CreatePlayer")>]
[<InlineData("Init")>]
[<InlineData("add")>]
[<InlineData(" add ")>]
let ``Should be an invalid command`` (rawCommand:string) =
    let expected = Error' "Invalid command"
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("add CreatePlayer")>]
[<InlineData(" add CreatePlayer ")>]
[<InlineData("add      CreatePlayer")>]
let ``Should be an add command`` (rawCommand:string) =
    let expected = Ok' (AddCommand "CreatePlayer")
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("update CreatePlayer")>]
[<InlineData(" update CreatePlayer ")>]
[<InlineData("update      CreatePlayer")>]
let ``Should be an update command`` (rawCommand:string) =
    let expected = Ok' (UpdateCommand (Some "CreatePlayer"))
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("update")>]
[<InlineData(" update ")>]
let ``Should be an update command with no entry name`` (rawCommand:string) =
    let expected = Ok' (UpdateCommand None)
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>

[<Theory>]
[<InlineData("remove")>]
[<InlineData(" remove ")>]
let ``Should be a remove command`` (rawCommand:string) =
    let expected = Ok' RemoveCommand
    let actual = CommandParser.parseCommand rawCommand
    test <@ actual = expected @>