namespace Shift

module CommandHandler =

    open System.IO
    open Shift
    open Shift.Common
    open Shift.MigrationRepository

    let getRepositoryDirInfo : ProjectDirectory 
                            -> MigrationRepositoryName 
                            -> MigrationRepositoryDirectory =
        fun projDir repoName ->
        { Name = repoName
          FullPath = Path.Combine(projDir.FullPath, repoName)}

    let migrationRepositoryExists : MigrationRepositoryDirectory -> bool =
        fun dir -> Directory.Exists dir.FullPath

    let createRepository : MigrationRepositoryDirectory -> unit =
        fun projDir -> 
        projDir.FullPath |> Directory.CreateDirectory |> ignore

    type InitializeHandlerError = string

    let initializeHandler : ProjectDirectory -> MigrationRepositoryName -> Result<unit, InitializeHandlerError> =
        fun projDir repoName -> 
        let repoDir = getRepositoryDirInfo projDir repoName
        if migrationRepositoryExists repoDir
        then Error "Migration repository already exists"
        else createRepository repoDir |> Ok

    type AddMigrationHandlerError =
        | MigrationIdError of MigrationId.MigrationIdError

    let addMigrationHandler : TimeStamp
                        -> MigrationRepositoryDirectory 
                        -> MigrationEntryName 
                        -> Result<unit, AddMigrationHandlerError> =
        fun timestamp migrationRepositoryDirectory entryName -> 
        let (<!>) = Result.map
        let (<*>) = Result.apply
        let migId = MigrationId.create timestamp entryName
                    |> Result.mapError AddMigrationHandlerError.MigrationIdError
        createMigrationEntry <!> (Ok migrationRepositoryDirectory) <*> migId

    type LatestIdFromHistory = MigrationId option
    type LatestIdFromRepository = MigrationId option
    type TargetMigrationId = MigrationId option

    type DatabaseSyncCommand = 
        | Update of from: MigrationId * upto: MigrationId
        | Revert of from: MigrationId * upto: MigrationId
        | UpdateFromFirst of upto: MigrationId

    type UpdateDatabaseHandlerError = 
        | NoMigrationEntries
        | MissingMigrationEntries
        | AlreadyUpToDate

    let (|UpdateDatabase|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> DatabaseSyncCommand option =
        function
        | Some history, Some repository, Some target when history < repository && target = repository -> Update(from = history, upto = repository) |> Some
        | Some history, Some repository, Some target when history < repository && target < repository && target > history -> Update(from = history, upto = target) |> Some
        | None, Some repository, Some target when target < repository -> UpdateFromFirst(target) |> Some
        | Some history, Some repository, None when history < repository -> Update(from = history, upto = repository) |> Some
        | None, Some repository, None -> UpdateFromFirst(upto = repository) |> Some
        | _ -> None

    let (|RevertDatabase|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> DatabaseSyncCommand option =
        function
        | Some history, Some repository, Some target when history = repository && target < repository -> Revert(from = repository, upto = target) |> Some
        | Some history, Some repository, Some target when history < repository && target < repository && target < history -> Revert(from = history, upto = target) |> Some
        | _ -> None

    let (|NoMigrationEntriesExists|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | None, None, _ -> Some()
        | _ -> None

    let (|DatabaseIsUpToDate|_|) :  LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | Some history, Some repository, Some target when history = target ->  Some()
        | Some history, Some repository, None when history = repository -> Some()
        | None, Some repository, Some target when target = repository ->  Some()
        | _ -> None

    let (|MigrationEntriesAreMissing|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | Some history, None, _ -> Some()
        | Some history, Some migration, _  when history > migration -> Some()
        | _ -> None

    let updateDatabaseHandler : LatestIdFromHistory 
                             -> LatestIdFromRepository 
                             -> TargetMigrationId 
                             -> Result<DatabaseSyncCommand, UpdateDatabaseHandlerError> =
        fun latestIdFromHistory latestIdFromRepository targetMigrationId -> 
        match latestIdFromHistory, latestIdFromRepository, targetMigrationId with
        | UpdateDatabase cmd -> Ok cmd
        | RevertDatabase cmd -> Ok cmd
        | NoMigrationEntriesExists -> Error NoMigrationEntries
        | DatabaseIsUpToDate -> Error AlreadyUpToDate
        | MigrationEntriesAreMissing -> Error MissingMigrationEntries
        | _ -> invalidArg "Update Database" (sprintf "Latest id from history: %A, Latest id from repository: %A, Target migration id: %A" 
                                                     latestIdFromHistory latestIdFromRepository targetMigrationId)

                                                     