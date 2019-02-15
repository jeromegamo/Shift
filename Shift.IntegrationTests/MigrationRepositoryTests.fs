module Shift.IntegrationTests.MigrationRepositoryTests

open Xunit
open Swensen.Unquote
open Shift
open Shift.IntegrationTests.TestHelpers

[<Fact>]
let ``Should find most recent migration entry by name`` () = 
    let (<!>) = Option.map
    let (<*>) = Option.apply
    let migrationRepoName = "DummyShiftMigrations"
    let name = DirectoryHelper.getProjectName()
    let projectDirectory = DirectoryHelper.getProjectDirectory name 
    let migrationRepoDir = projectDirectory |> Option.map (appendMigrationRepoDir migrationRepoName)
    let actual = MigrationRepository.tryFindByName <!> migrationRepoDir <*> (Some "CreatePlayer")
                 |> Option.bind id

    test <@ actual = MigrationId.parse "20180101060000-CreatePlayer" @>