module Shift.UnitTests.UpdateDatabaseTests

open System
open Xunit
open Swensen.Unquote
open Shift
open Shift.CommandHandler

let Ok' o : Result<DatabaseSyncCommand, UpdateDatabaseHandlerError> = Ok o
let Error' e : Result<DatabaseSyncCommand, UpdateDatabaseHandlerError> = Error e

[<Fact>]
let ``Given no target migration, no migrations, no history, should return NoMigrationEntries error`` () = 
    let latestIdFromHistory = None
    let latestIdFromRepository = None
    let targetMigrationId = None
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Error' NoMigrationEntries
    test <@ actual = expected @>

[<Fact>]
let ``Given no target migration, no history, a latest id from repository, should return UpdateFromFirst`` () = 
    let latestIdFromHistory = None
    let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
    let uptoId = MigrationId.parse "20180101110000-SeedPlayer" |> Option.get
    let targetMigrationId = None
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Ok' (UpdateFromFirst uptoId)
    test <@ actual = expected @>

[<Fact>]
let ``Given no target migration, when latest migration is same as latest history, should return AlreadyUpToDate error`` () = 
    let latestIdFromHistory = MigrationId.parse "20180101060000-CreatePlayer"
    let latestIdFromRepository = MigrationId.parse "20180101060000-CreatePlayer"
    let targetMigrationId = None
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Error' AlreadyUpToDate 
    test <@ actual = expected @>

[<Theory>]
[<InlineData("20180101060000-CreatePlayer", "")>]
[<InlineData("20180101110000-SeedPlayer", "20180101070000-CreateItems")>]
let ``Given no target migration, latest history is later than latest migration from repository, should return MissingMigrationEntries error`` (history, repository) =
    let latestIdFromHistory = MigrationId.parse history
    let latestIdFromRepository = MigrationId.parse repository
    let targetMigrationId = None
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Error' MissingMigrationEntries 
    test <@ actual = expected @>

[<Fact>]
let ``Given no target migration, latest history is older than latest migration, should return Update from latest history upto latest migration`` () =
    let latestIdFromHistory = MigrationId.parse "20180101080000-CreateInventory" 
    let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer" 
    let targetMigrationId = None
    let from = latestIdFromHistory |> Option.get 
    let upto = latestIdFromRepository |> Option.get
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Ok'(Update(from,upto))
    test <@ actual = expected @>

[<Fact>]
let ``Given target migration is older than the latest history, should return Revert`` () =
    let latestIdFromHistory = MigrationId.parse "20180101090000-AddPlayerStatus" 
    let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
    let targetMigrationId =  MigrationId.parse "20180101070000-CreateItems"
    let from = latestIdFromHistory |> Option.get 
    let upto = targetMigrationId |> Option.get
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Ok'(Revert(from,upto))
    test <@ actual = expected @>

[<Fact>]
let ``Given target migration is later than the latest history, should return Update`` () =
    let latestIdFromHistory = MigrationId.parse "20180101070000-CreateItems"
    let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
    let targetMigrationId =  MigrationId.parse "20180101090000-AddPlayerStatus"
    let from = latestIdFromHistory |> Option.get 
    let upto = targetMigrationId |> Option.get
    let actual = CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
    let expected = Ok'(Update(from,upto))
    test <@ actual = expected @>

[<Fact>]
let ``Given target migration is later than the latest id from repository, should throw an exception`` () =
    let latestIdFromHistory = MigrationId.parse "20180101070000-CreateItems"
    let latestIdFromRepository = MigrationId.parse "20180101090000-AddPlayerStatus" 
    let targetMigrationId =  MigrationId.parse "20180101110000-SeedPlayer"
    let from = latestIdFromHistory |> Option.get 
    let upto = targetMigrationId |> Option.get

    raisesWith<System.ArgumentException> 
        <@ 
            CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
        @>