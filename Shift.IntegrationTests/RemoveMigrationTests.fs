module Shift.IntegrationTests.RemoveMigrationTests

open System
open System.IO
open System.Data
open Xunit
open Swensen.Unquote
open Shift
open Shift.IntegrationTests.TestHelpers
open Shift.Console.Program

let rec copy (source:DirectoryInfo) (destination:DirectoryInfo) =
    let copyDirectories (source:DirectoryInfo) = 
        (source, destination.CreateSubdirectory(source.Name))
    let copyFiles (source:FileInfo) = 
        let destination = Path.Combine(destination.FullName, source.Name)
        source.CopyTo(destination, true) |> ignore
    source.EnumerateDirectories()
    |> Seq.map copyDirectories
    |> Seq.iter (fun (source, destination) -> copy source destination)
    source.EnumerateFiles()
    |> Seq.iter copyFiles

///----------
let dbSetup dbName (dbcom:IDbCommand) =
    dropDatabase dbName dbcom
    createDatabase dbName dbcom
    useDatabase dbName dbcom
    createMigrationHistoryTable dbcom

let testSetup (executeScriptsBeforeSUT: ConnectionString -> unit)
                (dbName:string) 
                (migrationRepoName:string) =
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
    let connectionString = projectDirectory |> Option.bind getConnectionString
    let migrationRepositoryDirectory = 
        projectDirectory 
        |> Option.map appendTempFolder
        |> Option.map (appendMigrationRepoDir migrationRepoName)
    let (<!>) = Option.map
    let (<*>) = Option.apply
    Db.runNonTransactional <!> connectionString <*> Some (dbSetup dbName) |> ignore

    //Original DummyMigrationFiles Directory
    let source = projectDirectory
                |> Option.map (fun dir -> Path.Combine(dir.FullPath, "DummyShiftMigrations"))
                |> Option.map (fun path -> DirectoryInfo path)
    //isolated temp folder per test
    let destination = migrationRepositoryDirectory |> Option.map (fun dir -> DirectoryInfo dir.FullPath)
    //create a copy of the original migration files per test
    copy <!> source <*> destination |> ignore

    let targetDbConnString = connectionString |> Option.map (TestHelpers.replaceDbName dbName)

    executeScriptsBeforeSUT <!> targetDbConnString |> ignore

    let projectDirectory' = projectDirectory |> Option.asResult "Project directory not found"

    let targetDbConnString' = targetDbConnString |> Option.asResult "Connection string not found"
    let (<!>) = Result.map
    let (<*>) = Result.apply
    let actual = removeMigrationHandler <!> targetDbConnString' <*> projectDirectory' <*> (Ok migrationRepositoryDirectory) 
                 |> Result.bind id
    (actual, targetDbConnString)

[<Fact>]
let ``Should remove the latest migration file if not yet applied to database`` () = 
    let dbName = "DB6738eedd"
    let migrationRepoName = "Temp01"

    let executeScriptsBeforeSUT = fun _ -> ()

    let (actual, targetDbConnString) = testSetup executeScriptsBeforeSUT dbName migrationRepoName
    test <@ actual = (Ok "Migration entry removed") @>

[<Fact>]
let ``Should not remove the latest migration file if already applied to database`` () = 
    let dbName = "DB939b7322"
    let migrationRepoName = "Temp02"

    let insertMigrationIds dbcmd = 
        let rawScript = "insert into ShiftMigrationHistory(MigrationId)
                         values ('20180101060000-CreatePlayer'),
                                 ('20180101070000-CreateItems'),
                                 ('20180101080000-CreateInventory'),
                                 ('20180101090000-AddPlayerStatus'),
                                 ('20180101100000-SeedItemCategory'),
                                 ('20180101110000-SeedPlayer')"
        executeNonQuery dbcmd rawScript |> ignore

    let executeScriptsBeforeSUT = 
        (fun connectionString -> 
            Db.runTransactional connectionString insertMigrationIds)

    let (actual, targetDbConnString) = testSetup executeScriptsBeforeSUT dbName migrationRepoName
    test <@ actual = (Error "Migration entry is currently applied") @>