module Shift.IntegrationTests.UpdateDatabaseTests

open System
open System.IO
open System.Data
open System.Data.SqlClient
open Xunit
open Swensen.Unquote
open Shift
open Shift.Command
open Shift.IntegrationTests.TestHelpers

let Ok' o : Result<string, string> = Ok o
let Error' e : Result<string, string> = Error e

let (<!>) = Result.map
let (<*>) = Result.apply

type FileName = string
let readScriptFile : ProjectDirectory -> FileName -> IDbCommand -> unit = 
    fun projectDirectory fileName dbcmd ->
    projectDirectory |> (fun dir -> Path.Combine(dir.FullPath, fileName))
                     |> (fun file -> File.ReadAllText(file))
                     |> (fun script ->         
                            dbcmd.CommandText <- script
                            dbcmd.ExecuteNonQuery() |> ignore)

let updateDatabase : MigrationEntryName option -> DirectoryDependencies -> IDbCommand -> Result<string, string> =
    fun migrationEntryName dir dbcmd -> 
    let historyDeps = 
        { ensureHistoryTable = fun _ -> MigrationHistory.ensureMigrationHistoryTable dbcmd
          tryFindLatest = fun _ -> MigrationHistory.tryFindLatest dbcmd }
    let repositoryDeps =
            { tryFindByName = MigrationRepository.tryFindByName 
              tryFindLatest = MigrationRepository.tryFindLatest
              getUpFilesByRange = MigrationRepository.getUpFilesByRange
              getDownFilesByRange = MigrationRepository.getDownFilesByRange
              getUpFilesUntil = MigrationRepository.getUpFilesUntil }

    let executeMigrationScripts = Command.executeMigrationScripts dbcmd

    Command.updateDatabase 
        historyDeps
        repositoryDeps
        executeMigrationScripts
        dir.migrationRepositoryDirectory
        migrationEntryName

[<Fact>]
let ``Should apply all migration entries to the database`` () = 
    let dbName = "DBef554b23"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let (_, connDeps , actual) = setupTest (fun _ _ -> ()) 
                                            (updateDatabase migrationEntryName) 
                                            dbName 
                                            migrationRepoName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript connDeps.targetDbConnectionString (fun _ -> None) MigrationHistory.tryFindLatest

    test <@ actual = expected @>
    test <@ latestFromHistory = MigrationId.parse "20180101110000-SeedPlayer" @>

[<Fact>]
let ``Should apply all migration upto the given migration entry`` () =
    let dbName = "DB8da31c42"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let (_, connDeps , actual) = setupTest (fun _ _ -> ()) 
                                            (updateDatabase migrationEntryName) 
                                            dbName 
                                            migrationRepoName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript connDeps.targetDbConnectionString (fun _ -> None) MigrationHistory.tryFindLatest

    test <@ actual = expected @>
    test <@ latestFromHistory = MigrationId.parse "20180101080000-CreateInventory" @>

[<Fact>]
let ``Should revert migration down to the given migration entry`` () = 
    let dbName = "DB6589ed3b"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let createDbState dirDeps = readScriptFile dirDeps.projectDirectory "TestScript01.sql"
    
    let (_, connDeps , actual) = setupTest createDbState 
                                           (updateDatabase migrationEntryName) 
                                           dbName 
                                           migrationRepoName
    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript connDeps.targetDbConnectionString (fun _ -> None) MigrationHistory.tryFindLatest

    test <@ actual = expected @>
    test <@ latestFromHistory = MigrationId.parse "20180101080000-CreateInventory" @> 

[<Fact>]
let ``Should apply remaining unapplied migration entries`` () = 
    let dbName = "DBa7cba2e"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let createDbState dirDeps = readScriptFile dirDeps.projectDirectory "TestScript02.sql"

    let (_, connDeps , actual) = setupTest createDbState 
                                           (updateDatabase migrationEntryName) 
                                           dbName 
                                           migrationRepoName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript connDeps.targetDbConnectionString (fun _ -> None) MigrationHistory.tryFindLatest

    test <@ actual = expected @>
    test <@ latestFromHistory = MigrationId.parse "20180101110000-SeedPlayer" @>   