module Shift.IntegrationTests.RemoveMigrationTests

open System
open System.IO
open System.Data
open Xunit
open Swensen.Unquote
open Shift
open Shift.IntegrationTests.TestHelpers

let removeMigration : DirectoryDependencies -> IDbCommand -> Result<string, string> =
    fun dirDeps dbcmd ->

    let source = dirDeps.projectDirectory
                |> (fun dir -> Path.Combine(dir.FullPath, "DummyShiftMigrations"))
                |> (fun path -> DirectoryInfo path)
    //isolated temp folder per test
    let destination = dirDeps.migrationRepositoryDirectory |> (fun dir -> DirectoryInfo dir.FullPath)
    //create a copy of the original migration files per test
    copy source destination |> ignore

    Command.remove MigrationRepository.tryFindLatest
                   (MigrationHistory.isMigrationApplied dbcmd)
                   MigrationRepository.deleteById
                   dirDeps.migrationRepositoryDirectory
[<Fact>]
let ``Should remove the latest migration file if not yet applied to database`` () = 
    let dbName = "DB6738eedd"
    let migrationRepoName = "Temp/Temp01"

    let (_, _, actual) = setupTest (fun _ _ -> ())
                                   removeMigration
                                   dbName
                                   migrationRepoName
                                                 
    test <@ actual = (Ok "Migration entry successfully deleted") @>

[<Fact>]
let ``Should not remove the latest migration file if already applied to database`` () = 
    let dbName = "DB939b7322"
    let migrationRepoName = "Temp/Temp02"

    let insertMigrationIds _ dbcmd = 
        let rawScript = "insert into ShiftMigrationHistory(MigrationId)
                      values ('20180101060000-CreatePlayer'),
                              ('20180101070000-CreateItems'),
                              ('20180101080000-CreateInventory'),
                              ('20180101090000-AddPlayerStatus'),
                              ('20180101100000-SeedItemCategory'),
                              ('20180101110000-SeedPlayer')"
        executeNonQuery dbcmd rawScript |> ignore

    let (_, _, actual) = setupTest insertMigrationIds
                                   removeMigration
                                   dbName
                                   migrationRepoName
                                   
    test <@ actual = (Error "Migration entry is currently applied.") @>