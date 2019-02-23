module Shift.IntegrationTests.AddMigrationTests

open System
open System.Reflection
open System.IO
open Xunit
open Swensen.Unquote
open Shift
open Shift.Command
open Shift.IntegrationTests.TestHelpers

let Ok' o : Result<string, string> = Ok o
let Error' e : Result<string, string> = Error e

let addMigration' : GetTimeStamp 
                 -> CreateMigrationEntry
                 -> MigrationRepositoryName 
                 -> MigrationEntryName 
                 -> Result<string, string> =
    fun getTimeStamp createMigrationEntry migrationRepositoryName entryName ->
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> Option.asResult "Project directory not found"
                           |> Result.map appendTempFolder
    let migrationRepoDir = projectDirectory
                           |> Result.map (appendMigrationRepoDir migrationRepositoryName)
                           |> Result.map (fun dir -> Directory.CreateDirectory(dir.FullPath) |> ignore
                                                     dir)
    let (<!>) = Result.map
    let (<*>) = Result.apply

    let actual = Command.addMigration <!> Ok getTimeStamp <*> Ok createMigrationEntry <*> migrationRepoDir <*> Ok entryName
                 |> Result.bind id

    migrationRepoDir |> function Ok dir -> Directory.Delete (dir.FullPath, true) | Error e -> e |> ignore

    actual

[<Fact>]
let ``Should create a migration entry`` () = 
    let migrationRepositoryName = "MigrationRepository-e51947ff"
    let getTimeStamp = fun () -> DateTime(2018,01,01,06,00,00)
    let migrationEntryName = "CreatePlayer"
    let createMigrationEntry = fun _ _ -> ()
    let expected = Ok' "Migration entry created"
    let actual = addMigration' getTimeStamp createMigrationEntry migrationRepositoryName migrationEntryName 
    <@ actual = expected @>