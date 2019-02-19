module Shift.IntegrationTests.InitializationTests

open System
open System.IO
open System.Reflection
open Xunit
open Shift.LanguageExtensions
open Shift.Common
open Shift.DirectoryHelper
open Shift.CommandHandler
open Shift
open Swensen.Unquote
open Shift.IntegrationTests.TestHelpers

let ( <!> ) = Option.map
let ( <*> ) = Option.apply

let Ok' : unit -> Result<unit, InitializeHandlerError> = Ok
let Error' : InitializeHandlerError -> Result<unit, InitializeHandlerError> = Error

[<Fact>]
let ``Should create migration repository`` () =
    let migrationRepositoryName = "b721a7d7-412c-4208-9ae4-fb221f77d726"
    let name = getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> Option.map appendTempFolder
    let migrationRepoDir = projectDirectory
                           |> Option.map (appendMigrationRepoDir migrationRepositoryName)

    let  actual = initializeHandler <!> projectDirectory <*> (Some migrationRepositoryName)

    test <@ actual = Some (Ok' ()) @>
    test <@
            match migrationRepoDir with
            | None -> false
            | Some v -> Directory.Exists v.FullPath 
         @>

    migrationRepoDir |> Option.iter (fun dir -> Directory.Delete (dir.FullPath, true) |> ignore)

[<Fact>]
let ``Should return error if migration repository already exists`` () =
    let migrationRepositoryName = "c3c67984-9262-4df5-85f0-628b68d2dcfc"
    let name = getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> Option.map appendTempFolder
    let migrationRepoDir = projectDirectory
                           |> Option.map (appendMigrationRepoDir migrationRepositoryName)

    migrationRepoDir |> Option.iter (fun dir -> Directory.CreateDirectory(dir.FullPath) |> ignore)

    let  actual = initializeHandler <!> projectDirectory <*> (Some migrationRepositoryName)

    test <@ actual = Some (Error' "Migration repository already exists") @>

    migrationRepoDir |> Option.iter (fun dir -> Directory.Delete (dir.FullPath, true) |> ignore)