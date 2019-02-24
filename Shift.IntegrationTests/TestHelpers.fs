module Shift.IntegrationTests.TestHelpers

open System
open System.IO
open System.Data
open System.Data.SqlClient
open System.Reflection
open Shift

let appendTempFolder : ProjectDirectory -> ProjectDirectory =
    fun dir -> 
    let tempName = "Temp"
    let tempDir = { ProjectDirectory.Name = tempName
                    FullPath = Path.Combine(dir.FullPath, tempName) }
    Directory.CreateDirectory tempDir.FullPath |> ignore
    tempDir

let appendMigrationRepoDir : MigrationRepositoryName -> ProjectDirectory -> ProjectDirectory =
    fun repoName dir -> 
    { ProjectDirectory.Name = repoName
      FullPath = Path.Combine(dir.FullPath, repoName) }

let createDatabase (dbName:string) (cmd:IDbCommand) =
    let rawScript = String.Format("if db_id('{0}') is not null
                                       drop database {0}
                                   create database {0}", dbName)
    executeNonQuery cmd rawScript |> ignore

let useDatabase (dbName:string) (cmd:IDbCommand) =
    let rawScript = String.Format("use {0}", dbName)
    executeNonQuery cmd rawScript |> ignore

let createMigrationHistoryTable (cmd:IDbCommand) =
    let rawScript = @"create table ShiftMigrationHistory(
                          MigrationId varchar(114),
                          constraint PK_MigrationId primary key (MigrationId)
                      )"
    executeNonQuery cmd rawScript |> ignore

let dropDatabase (dbName:string) (cmd:IDbCommand) =
    let rawScript = String.Format("if db_id('{0}') is not null
                                       drop database {0}", dbName)
    executeNonQuery cmd rawScript |> ignore

let replaceDbName dbName (connString:string) =
    let connArray = connString.Split(';')
    let dbIndex = connArray |> Seq.findIndex (fun (s:string) -> s.Contains("Database"))
    connArray.[dbIndex] <- sprintf "Database=%s" dbName
    String.concat ";" connArray 

let dbSetup dbName (dbcom:IDbCommand) =
    dropDatabase dbName dbcom
    createDatabase dbName dbcom
    useDatabase dbName dbcom
    createMigrationHistoryTable dbcom


type DbName = string
type MasterConnectionString = ConnectionString
type TargetDbConnectionString = ConnectionString
type DirectoryDependencies = 
    { projectDirectory : ProjectDirectory
      migrationRepositoryDirectory : MigrationRepositoryDirectory }
type ConnectionStringDependencies = 
    { masterConnectionString : MasterConnectionString
      targetDbConnectionString : TargetDbConnectionString }

let getDirectoryDependencies : MigrationRepositoryName -> DirectoryDependencies =
    fun migrationRepoName ->
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> function 
                           | None -> failwith "Cannot find test project directory"
                           | Some dir -> dir
    let migrationRepositoryDirectory = projectDirectory |> appendMigrationRepoDir migrationRepoName
    { projectDirectory = projectDirectory
      migrationRepositoryDirectory = migrationRepositoryDirectory }

let getConnStringDependencies : ProjectDirectory -> DbName -> ConnectionStringDependencies =
    fun projectDirectory dbName ->
    let connectionString  = getConnectionString projectDirectory 
                            |> function None -> failwith "Cannot find connection string" | Some conn -> conn
    let targetDbConnString = connectionString |> replaceDbName dbName 
    { masterConnectionString = connectionString; targetDbConnectionString = targetDbConnString }

type ExecuteBeforeSystemUnderTest = DirectoryDependencies -> IDbCommand -> unit
type ExecuteSystemUnderTest<'r> = DirectoryDependencies -> IDbCommand -> 'r
let setupTest : ExecuteBeforeSystemUnderTest
              -> ExecuteSystemUnderTest<'r>
              -> DbName 
              -> MigrationRepositoryName 
              -> DirectoryDependencies * ConnectionStringDependencies * 'r=
    fun executeBefore executeSystemUnderTest dbName migrationRepositoryName ->
    let dirDeps = getDirectoryDependencies migrationRepositoryName
    let connStringDeps = getConnStringDependencies dirDeps.projectDirectory dbName
    executeDbScript connStringDeps.masterConnectionString ignoreDbTransaction (dbSetup dbName) |> ignore
    executeDbScript connStringDeps.targetDbConnectionString getDbTransaction (executeBefore dirDeps) |> ignore
    let result = executeDbScript connStringDeps.targetDbConnectionString getDbTransaction (executeSystemUnderTest dirDeps)
    (dirDeps, connStringDeps, result)


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