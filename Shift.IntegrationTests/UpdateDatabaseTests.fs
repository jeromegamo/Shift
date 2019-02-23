module Shift.IntegrationTests.UpdateDatabaseTests

open System
open System.IO
open System.Data
open System.Data.SqlClient
open Xunit
open Swensen.Unquote
open Shift
open Shift.Command

let Ok' o : Result<string, string> = Ok o
let Error' e : Result<string, string> = Error e

type GetDbTransaction = SqlConnection -> SqlTransaction option
let executeDbScript : ConnectionString -> GetDbTransaction -> (SqlCommand -> 'r) -> 'r =
    fun connectionString getTransaction execute ->
    use connection = new SqlConnection(connectionString)
    connection.Open()
    let tx = getTransaction connection
    let cmd = connection.CreateCommand()
    match tx with
    | None ->
        try execute cmd 
        with _ -> reraise()
    | Some tx ->
        cmd.Transaction <- tx
        try
            let result = execute cmd
            tx.Commit()
            connection.Close()
            result
        with _ -> 
            tx.Rollback()
            connection.Close()
            reraise()

let (<!>) = Result.map
let (<*>) = Result.apply

let dbSetup dbName (dbcom:IDbCommand) =
    TestHelpers.dropDatabase dbName dbcom
    TestHelpers.createDatabase dbName dbcom
    TestHelpers.useDatabase dbName dbcom
    TestHelpers.createMigrationHistoryTable dbcom

type FileName = string
let readScriptFile : ProjectDirectory -> FileName -> IDbCommand -> unit = 
    fun projectDirectory fileName dbcmd ->
    projectDirectory |> (fun dir -> Path.Combine(dir.FullPath, fileName))
                     |> (fun file -> File.ReadAllText(file))
                     |> (fun script ->         
                            dbcmd.CommandText <- script
                            dbcmd.ExecuteNonQuery() |> ignore)

type DbName = string
let testUpdateDb : (ConnectionString -> ProjectDirectory -> unit) 
                -> DbName 
                -> MigrationRepositoryName 
                -> MigrationEntryName option 
                -> (Result<ConnectionString, string> * Result<string, string>) =
    fun executeBefore dbName migrationRepoName migrationEntryName ->
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                            |> Option.asResult "No project directory"
    let getConnectionString' dir =
        getConnectionString dir |> function None -> Error "No connection string" | Some conn -> Ok conn
    let connectionString = projectDirectory 
                            |> Result.bind getConnectionString'

    let targetDbConnString = connectionString |> Result.map (TestHelpers.replaceDbName dbName)

    let updateDb dbcmd = 
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

        let migrationRepositoryDirectory = projectDirectory |> Result.map (TestHelpers.appendMigrationRepoDir migrationRepoName)

        Command.updateDatabase 
            <!> (Ok historyDeps) 
            <*> (Ok repositoryDeps) 
            <*> (Ok executeMigrationScripts) 
            <*> migrationRepositoryDirectory 
            <*> Ok (migrationEntryName)
            |> Result.bind id

    executeDbScript <!> connectionString <*> Ok (fun _ -> None ) <*> Ok (dbSetup dbName) |> ignore
    executeBefore <!> targetDbConnString <*> projectDirectory |> ignore 
    let result = executeDbScript <!> targetDbConnString <*> Ok (fun conn -> Some (conn.BeginTransaction())) <*> Ok updateDb
                 |> Result.bind id

    (targetDbConnString, result)

[<Fact>]
let ``Should apply all migration entries to the database`` () = 
    let dbName = "DBef554b23"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let (targetDbConnString, actual) = testUpdateDb (fun _ _ -> ()) dbName migrationRepoName migrationEntryName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript <!> targetDbConnString <*> Ok (fun _ -> None) <*> (Ok MigrationHistory.tryFindLatest)

    test <@ actual = expected @>
    test <@ latestFromHistory = Ok (MigrationId.parse "20180101110000-SeedPlayer") @>

[<Fact>]
let ``Should apply all migration upto the given migration entry`` () =
    let dbName = "DB8da31c42"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let (targetDbConnString, actual) = testUpdateDb (fun _ _ -> ()) dbName migrationRepoName migrationEntryName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript <!> targetDbConnString <*> Ok (fun _ -> None) <*> (Ok MigrationHistory.tryFindLatest)

    test <@ latestFromHistory = Ok (MigrationId.parse "20180101080000-CreateInventory") @>

[<Fact>]
let ``Should revert migration down to the given migration entry`` () = 
    let dbName = "DB6589ed3b"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = Some "CreateInventory"

    let createDbState connectionString projectDirectory = 
        executeDbScript connectionString (fun _ -> None) (readScriptFile projectDirectory "TestScript01.sql") 

    let (targetDbConnString, actual) = testUpdateDb createDbState dbName migrationRepoName migrationEntryName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript <!> targetDbConnString <*> Ok (fun _ -> None) <*> (Ok MigrationHistory.tryFindLatest)

    test <@ latestFromHistory = Ok (MigrationId.parse "20180101080000-CreateInventory") @>

[<Fact>]
let ``Should apply remaining unapplied migration entries`` () = 
    let dbName = "DBa7cba2e"
    let migrationRepoName = "DummyShiftMigrations"
    let migrationEntryName = None

    let createDbState connectionString projectDirectory = 
        executeDbScript connectionString (fun _ -> None) (readScriptFile projectDirectory "TestScript02.sql") 

    let (targetDbConnString, actual) = testUpdateDb createDbState dbName migrationRepoName migrationEntryName

    let expected = Ok' "Database is successfully updated"

    let latestFromHistory = executeDbScript <!> targetDbConnString <*> Ok (fun _ -> None) <*> (Ok MigrationHistory.tryFindLatest)

    test <@ latestFromHistory = Ok (MigrationId.parse "20180101110000-SeedPlayer") @>