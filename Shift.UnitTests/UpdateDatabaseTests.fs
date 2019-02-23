namespace Shift.UnitTests

module UpdateTests =

    open System
    open Xunit
    open Swensen.Unquote
    open Shift
    open Shift.Command

    let migrationRepositoryDirectory = { ProjectDirectory.Name = ""
                                         FullPath = ""}
    let historyDeps = 
        { ensureHistoryTable = fun _ -> ()
          tryFindLatest = fun _ -> None }
    let repositoryDeps =
            { tryFindByName = fun _ _ -> None 
              tryFindLatest = fun _ -> None 
              getUpFilesByRange = fun _ _ _ -> Seq.empty
              getDownFilesByRange = fun _ _ _ -> Seq.empty
              getUpFilesUntil = fun _ _ -> Seq.empty }
    let execMigSx = fun _ -> ()

    let Ok' o : Result<string, string> = Ok o
    let Error' e : Result<string, string> = Error e

    [<Fact>]
    let ``Given no target id provided, no latest id from repository, no latest id from history in db, should return a no migration entries error`` () = 
        let actual = Command.updateDatabase historyDeps repositoryDeps execMigSx migrationRepositoryDirectory (Some "")
        let expected = Error' "No migration entries"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given no target id provided, a latest id from repository, no latest id from history in db, should successfully update database`` () = 
        let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository }
        let actual = Command.updateDatabase historyDeps repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Ok' "Database is successfully updated"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given no target id provided, when latest id from repository equal to the latest id from history in db, should return an already up-to-date error`` () = 
        let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
        let latestIdFromHistory = MigrationId.parse "20180101110000-SeedPlayer"
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        let actual = Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Error' "Database is up-to-date"
        test <@ actual = expected @>

    [<Theory>]
    [<InlineData("20180101060000-CreatePlayer", "")>]
    [<InlineData("20180101110000-SeedPlayer", "20180101070000-CreateItems")>]
    let ``Given no target id provided, latest id from history is later than latest id from repository, should return a migration entries are missing error`` (history, repository) =
        let latestIdFromHistory = MigrationId.parse history
        let latestIdFromRepository = MigrationId.parse repository
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        let actual = Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Error' "Migration entries are missing"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given no target id provided, latest id from history history is older than latest id from repository, should successfully update the database`` () =
        let latestIdFromHistory = MigrationId.parse "20180101080000-CreateInventory" 
        let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer" 
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        let actual = Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Ok' "Database is successfully updated"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given a target id is older than the latest id from history, should successfully update the database`` () =
        let latestIdFromHistory = MigrationId.parse "20180101090000-AddPlayerStatus" 
        let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
        let targetMigrationId =  MigrationId.parse "20180101070000-CreateItems"
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository 
                                                    tryFindByName = fun _ _ -> targetMigrationId }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        let actual = Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Ok' "Database is successfully updated"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given target id is later than the latest id from history, should successfully update the database`` () =
        let latestIdFromHistory = MigrationId.parse "20180101070000-CreateItems"
        let latestIdFromRepository = MigrationId.parse "20180101110000-SeedPlayer"
        let targetMigrationId =  MigrationId.parse "20180101090000-AddPlayerStatus"
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository 
                                                    tryFindByName = fun _ _ -> targetMigrationId }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        let actual = Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "")
        let expected = Ok' "Database is successfully updated"
        test <@ actual = expected @>

    [<Fact>]
    let ``Given target id is later than the latest id from repository, should throw an invalid argument exception`` () =
        let latestIdFromHistory = MigrationId.parse "20180101070000-CreateItems"
        let latestIdFromRepository = MigrationId.parse "20180101090000-AddPlayerStatus" 
        let targetMigrationId =  MigrationId.parse "20180101110000-SeedPlayer"
        let repositoryDeps' = { repositoryDeps with tryFindLatest = fun _ -> latestIdFromRepository 
                                                    tryFindByName = fun _ _ -> targetMigrationId }
        let historyDeps' = { historyDeps with tryFindLatest = fun _ -> latestIdFromHistory }
        raisesWith<System.ArgumentException> <@ Command.updateDatabase historyDeps' repositoryDeps' execMigSx migrationRepositoryDirectory (Some "") @>