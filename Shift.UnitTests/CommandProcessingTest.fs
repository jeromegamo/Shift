module Shift.UnitTests.CommandProcessingTest

open System
open Xunit
open Swensen.Unquote
open Shift
open Shift.Command
open Shift.MigrationRepository

let Ok' v : Result<string, string> = Ok v
let Error' v : Result<string, string> = Error v

let processCommandDeps =
    { initializeMigration = fun _ -> Ok "Migration repository created at"
      addMigration = fun _ _ -> Ok "Migration entry created"
      updateDatabase = fun _ _ _ -> Ok "Database is successfully updated"
      removeMigration = fun _ _ -> Ok "Migration entry successfully deleted"}

let migrationRepositoryDirectory = { Name = ""
                                     FullPath = ""}
let getConnectionString = fun _ -> None
let ensureMigrationHistoryTable = fun _ -> ()

[<Fact>]
let ``When command is init, and repository exist, should return error`` () = 
    let migrationRepositoryState = Exist migrationRepositoryDirectory
    let parsedCommand = InitCommand
    let actual = processCommand getConnectionString 
                                processCommandDeps
                                ensureMigrationHistoryTable 
                                migrationRepositoryState 
                                parsedCommand
    test <@ actual = Error' "A repository already exists" @>

[<Fact>]
let ``When command is not init, and repository does not exist, should return error`` () = 
    let migrationRepositoryState = DoesNotExist migrationRepositoryDirectory
    let parsedCommand = AddCommand "SeedPlayerTable"
    let actual = processCommand getConnectionString 
                                processCommandDeps
                                ensureMigrationHistoryTable 
                                migrationRepositoryState 
                                parsedCommand
    test <@ actual = Error' "Migration repository does not exist" @>

[<Fact>]
let ``When command is update, should return error if no connection string is found`` () = 
    let migrationRepositoryState = Exist migrationRepositoryDirectory
    let parsedCommand = UpdateCommand (Some "SeedPlayerTable")
    let actual = processCommand getConnectionString 
                                processCommandDeps
                                ensureMigrationHistoryTable 
                                migrationRepositoryState 
                                parsedCommand
    test <@ actual = Error' "Cannot find connection string" @>