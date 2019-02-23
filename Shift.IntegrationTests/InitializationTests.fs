module Shift.IntegrationTests.InitializationTests

open System
open System.IO
open System.Reflection
open Xunit
open Shift
open Shift.IntegrationTests.TestHelpers
open Swensen.Unquote

let ( <!> ) = Option.map

let Ok' o : Result<string, string> = Ok o
let Error' e : Result<string, string> = Error e

[<Fact>]
let ``Should create migration repository`` () =
    let migrationRepositoryName = "MigrationRepository-b721a7d7"
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
                           |> Option.map appendTempFolder
    let migrationRepoDir = projectDirectory
                           |> Option.map (appendMigrationRepoDir migrationRepositoryName)

    let  actual = Command.initializeMigration <!> migrationRepoDir 

    let actual' = match actual with
                  | Some result -> result
                  | None -> failwith "invalid result"
    let expected = match migrationRepoDir with
                   | Some dir -> Ok' (sprintf "Migration repository created at %A" dir.FullPath) 
                   | None -> failwith "invalid result" 
    test <@ actual' = expected @>
    test <@
            match migrationRepoDir with
            | None -> false
            | Some dir -> Directory.Exists dir.FullPath 
         @>

    migrationRepoDir |> Option.iter (fun dir -> Directory.Delete (dir.FullPath, true) |> ignore)
