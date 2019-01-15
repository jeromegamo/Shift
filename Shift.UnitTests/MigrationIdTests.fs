module Shift.UnitTests.MigrationIdTests

open System
open Xunit
open Swensen.Unquote
open Shift

[<Fact>]
let ``Created migrationIds, should be equal`` () =
    let timestamp = DateTime(2018,01,01,11,00,00)
    let left = MigrationId.create timestamp "SeedPlayer"
    let right = MigrationId.create timestamp "SeedPlayer"
    left =! right

[<Fact>]
let ``Created migrationIds, should be comparable`` () =
    let leftTimestamp = DateTime(2018,01,01,09,00,00)
    let left = MigrationId.create leftTimestamp "AddPlayerStatus"
    let rightTimestamp = DateTime(2018,01,01,11,00,00)
    let right = MigrationId.create rightTimestamp "SeedPlayer"
    left <! right
    right >! left

[<Fact>]
let ``Given no name, should return Required error`` () = 
    let timestamp = DateTime(2018,01,01,09,00,00)
    let actual = MigrationId.create timestamp ""
    let expected = Error Required
    actual =! expected

[<Fact>]
let ``Given no name, should return MinLength error`` () = 
    let timestamp = DateTime(2018,01,01,09,00,00)
    let actual = MigrationId.create timestamp "Abc"
    let expected = Error MinLength
    actual =! expected

[<Fact>]
let ``Given no name, should return MaxLength error`` () = 
    let longName = Seq.init 101 (fun _ -> 'a') |> Seq.fold (fun acc i -> acc + string i) ""
    let timestamp = DateTime(2018,01,01,09,00,00)
    let actual = MigrationId.create timestamp longName
    let expected = Error MaxLength
    actual =! expected

[<Fact>]
let ``Given a MigrationId in string form, when parsed, should return the MigrationId`` () =
   let mIdstring = "20180101100000-SeedItemCategory"
   let timestamp = DateTime(2018,01,01,10,00,00)
   let expected = MigrationId.create timestamp "SeedItemCategory" |> Result.getOk
   let actual = MigrationId.parse mIdstring |> Option.get
   actual =! expected

[<Fact>]
let ``Given a directory of a migration, when parsed, should return the MigrationId`` () =
   let dir = "/ProjectDirectory/Migrations/20180101060000-CreatePlayer"
   let timestamp = DateTime(2018,01,01,06,00,00)
   let expected = MigrationId.create timestamp "CreatePlayer" |> Result.getOk
   let actual = MigrationId.parse dir |> Option.get
   actual =! expected

[<Fact>]
let ``Given some random string, when parsed, should return none`` () =
   let dir = "/Some/Other/Directory/Or/Random/String"
   let timestamp = DateTime(2018,01,01,06,00,00)
   let expected = None
   let actual = MigrationId.parse dir
   actual =! expected