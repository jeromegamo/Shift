module Shift.IntegrationTests.AddMigrationTests

open Xunit
open Swensen.Unquote
open System
open System.Reflection
open System.IO
open Shift
open Shift.Common
open Shift.CommandHandler
open Shift.DirectoryHelper
open Shift.IntegrationTests.TestHelpers

let Ok' : unit -> Result<unit, AddMigrationHandlerError> = Ok
let Error' : AddMigrationHandlerError -> Result<unit, AddMigrationHandlerError> = Error

let setup' : TimeStamp 
         -> MigrationRepositoryName 
         -> MigrationEntryName
         -> (Result<unit, AddMigrationHandlerError> -> unit)
         -> unit = 
    fun timestamp migrationRepositoryName migrationEntryName executeTest->
    let name = getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> Option.map appendTempFolder
    let migrationRepoDir = projectDirectory
                           |> Option.map (appendMigrationRepoDir migrationRepositoryName)
                           |> Option.map (fun dir -> Directory.CreateDirectory(dir.FullPath) |> ignore
                                                     dir)
    let (<!>) = Option.map
    let (<*>) = Option.apply
    let actual = addMigrationHandler <!> (Some timestamp) <*> migrationRepoDir <*> (Some migrationEntryName)
                 |> Option.get

    executeTest actual

    migrationRepoDir |> Option.iter (fun dir -> Directory.Delete (dir.FullPath, true) |> ignore)

[<Fact>]
let ``Should create a migration entry`` () = 
    let migrationRepositoryName = "e51947ff-1464-4e75-b209-ab9b17642b50"
    let timestamp = DateTime(2018,01,01,06,00,00)
    let migrationEntryName = "CreatePlayer"
    let expected = Ok' ()
    setup' timestamp migrationRepositoryName migrationEntryName 
           <| fun actual -> 
              test <@ actual = expected @>