module Shift.IntegrationTests.TestHelpers

open System
open System.IO
open System.Reflection
open System.Data
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