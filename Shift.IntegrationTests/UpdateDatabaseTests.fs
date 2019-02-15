module Shift.IntegrationTests.UpdateDatabaseTests

open System
open System.Configuration
open System.Data
open System.IO
open Xunit
open Swensen.Unquote
open Shift
open Shift.Db
open Shift.Console
open Shift.CommandHandler
open Shift.IntegrationTests.TestHelpers

let Ok' o : Result<unit, UpdateDatabaseHandlerError> = Ok o
let Error' e : Result<unit, UpdateDatabaseHandlerError> = Error e

let dbSetup dbName (dbcom:IDbCommand) =
    dropDatabase dbName dbcom
    createDatabase dbName dbcom
    useDatabase dbName dbcom
    createMigrationHistoryTable dbcom


let (<!>) = Option.map
let (<*>) = Option.apply

let testSetup (executeScriptsBeforeSUT: ConnectionString -> ProjectDirectory -> unit)
              (dbName:string) 
              (migrationRepoName:string) 
              (migrationEntryName:MigrationEntryName option) =
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
    let connectionString = projectDirectory |> Option.bind getConnectionString
    let migrationRepoDir = projectDirectory |> Option.map (appendMigrationRepoDir migrationRepoName)

    Db.runNonTransactional <!> connectionString <*> Some (dbSetup dbName) |> ignore

    let targetDbConnString = connectionString |> Option.map (TestHelpers.replaceDbName dbName)

    executeScriptsBeforeSUT <!> targetDbConnString |> ignore

    let actual = UpdateHandler.updateController <!> targetDbConnString <*> migrationRepoDir <*> (Some migrationEntryName)
    (actual, targetDbConnString)

[<Fact>]
let ``Should apply all migration entries to the database`` () = 

    let dbName = "DBef554b23"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let (actual, targetDbConnString) = testSetup (fun _ _ -> ()) dbName migrationRepoName migrationEntryName

    test <@ match actual with
            | None -> false
            | Some actual -> actual = Ok'() 
            @>

    let latestFromHistory = Db.runNonTransactional <!> targetDbConnString <*> (Some MigrationHistory.tryFindLatest)
                            |> Option.bind id

    test <@ latestFromHistory = MigrationId.parse "20180101110000-SeedPlayer" @>

[<Fact>]
let ``Should apply all migration upto the given migration entry`` () =
    let dbName = "DB8da31c42"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let (actual, targetDbConnString) = testSetup (fun _ _ -> ())  dbName migrationRepoName migrationEntryName

    test <@ match actual with
            | None -> false
            | Some actual -> actual = Ok'() 
            @>

    let latestFromHistory = Db.runNonTransactional <!> targetDbConnString <*> (Some MigrationHistory.tryFindLatest)
                            |> Option.bind id
    test <@ latestFromHistory = MigrationId.parse "20180101080000-CreateInventory" @>

[<Fact>]
let ``Should revert migration down to the given migration entry`` () = 
    let dbName = "DB6589ed3b"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let createDbState connectionString projectDirectory =
        Db.runTransactional connectionString (
            fun dbcmd -> 
                projectDirectory |> (fun dir -> Path.Combine(dir.FullPath, "TestScript01.sql"))
                                 |> (fun file -> File.ReadAllText(file))
                                 |> (fun script ->         
                                        dbcmd.CommandText <- script
                                        dbcmd.ExecuteNonQuery() |> ignore)
            )   |> ignore

    let (actual, targetDbConnString) = testSetup createDbState dbName migrationRepoName migrationEntryName

    test <@ match actual with
            | None -> false
            | Some actual -> actual = Ok'() 
            @>
    let latestFromHistory = Db.runNonTransactional <!> targetDbConnString <*> (Some MigrationHistory.tryFindLatest)
                            |> Option.bind id
    test <@ latestFromHistory = MigrationId.parse "20180101080000-CreateInventory" @>

[<Fact>]
let ``Should apply remaining unapplied migration entries`` () = 
    let dbName = "DBa7cba2e"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let createDbState connectionString projectDirectory =
        Db.runTransactional connectionString (
            fun dbcmd -> 
                projectDirectory |> (fun dir -> Path.Combine(dir.FullPath, "TestScript02.sql"))
                                 |> (fun file -> File.ReadAllText(file))
                                 |> (fun script ->         
                                        dbcmd.CommandText <- script
                                        dbcmd.ExecuteNonQuery() |> ignore)
            )   |> ignore

    let (actual, targetDbConnString) = testSetup createDbState dbName migrationRepoName migrationEntryName

    test <@ match actual with
            | None -> false
            | Some actual -> actual = Ok'() 
            @>
    let latestFromHistory = Db.runNonTransactional <!> targetDbConnString <*> (Some MigrationHistory.tryFindLatest)
                            |> Option.bind id
    test <@ latestFromHistory = MigrationId.parse "20180101110000-SeedPlayer" @>