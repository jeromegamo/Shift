namespace Shift

open System
open System.IO
open System.Data
open Shift.MigrationRepository

module Command =
    type InitializeMigration = MigrationRepositoryDirectory -> Result<string, string>

    let initializeMigration : InitializeMigration =
        fun repo -> 
        let dirinfo = repo.FullPath |> Directory.CreateDirectory
        if dirinfo.Exists then Ok (sprintf "Migration repository created at %A" repo.FullPath)
        else Error "Unable to create repository" 

    type AddMigration = MigrationRepositoryDirectory -> MigrationEntryName -> Result<string, string>
    type CreateMigrationEntry = MigrationRepositoryDirectory -> MigrationId -> unit
    type GetTimeStamp = unit -> DateTime

    let addMigration : GetTimeStamp 
                    -> CreateMigrationEntry
                    -> AddMigration = 
        fun getTimeStamp createMigrationEntry migrationRepositoryDirectory entryName ->
            let timestamp = getTimeStamp()
            MigrationId.create timestamp entryName
            |> function
            | Ok mId -> Ok mId
            | Error Required -> Error "An entry name is required"
            | Error MaxLength -> Error "Entry name should have a max length of 100"
            | Error MinLength -> Error "Entry name should have a min length of 4"
            |> Result.map (createMigrationEntry migrationRepositoryDirectory)
            |> Result.map (fun _ -> "Migration entry created")

    type private LatestIdFromHistory = MigrationId option
    type private LatestIdFromRepository = MigrationId option
    type private TargetMigrationId = MigrationId option
    type DbUpdateAction = 
        | Update of from: MigrationId * upto: MigrationId
        | Revert of from: MigrationId * upto: MigrationId
        | UpdateFromFirst of upto: MigrationId

    let private (|ForUpdate|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> DbUpdateAction option =
        function
        | Some history, Some repository, Some target when history < repository && target = repository -> Update(from = history, upto = repository) |> Some
        | Some history, Some repository, Some target when history < repository && target < repository && target > history -> Update(from = history, upto = target) |> Some
        | None, Some repository, Some target when target < repository -> UpdateFromFirst(target) |> Some
        | Some history, Some repository, None when history < repository -> Update(from = history, upto = repository) |> Some
        | None, Some repository, None -> UpdateFromFirst(upto = repository) |> Some
        | _ -> None

    let private (|ForRevert|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> DbUpdateAction option =
        function
        | Some history, Some repository, Some target when history = repository && target < repository -> Revert(from = repository, upto = target) |> Some
        | Some history, Some repository, Some target when history < repository && target < repository && target < history -> Revert(from = history, upto = target) |> Some
        | _ -> None

    let private (|NoMigrationEntriesExists|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | None, None, _ -> Some()
        | _ -> None

    let private (|DatabaseIsUpToDate|_|) :  LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | Some history, Some repository, Some target when history = target ->  Some()
        | Some history, Some repository, None when history = repository -> Some()
        | None, Some repository, Some target when target = repository ->  Some()
        | _ -> None

    let private (|MigrationEntriesAreMissing|_|) : LatestIdFromHistory * LatestIdFromRepository * TargetMigrationId -> unit option =
        function
        | Some history, None, _ -> Some()
        | Some history, Some migration, _  when history > migration -> Some()
        | _ -> None

    let private getDbUpdateAction : LatestIdFromHistory -> LatestIdFromRepository -> TargetMigrationId -> Result<DbUpdateAction, string> =
        fun latestIdFromHistory latestIdFromRepository targetMigrationId ->
        match latestIdFromHistory, latestIdFromRepository, targetMigrationId with
        | ForUpdate cmd -> Ok cmd
        | ForRevert cmd -> Ok cmd
        | NoMigrationEntriesExists -> Error "No migration entries"
        | DatabaseIsUpToDate -> Error "Database is up-to-date"
        | MigrationEntriesAreMissing -> Error "Migration entries are missing"
        | _ -> invalidArg "Update Database" (sprintf "Latest id from history: %A, Latest id from repository: %A, Target migration id: %A" 
                                                    latestIdFromHistory latestIdFromRepository targetMigrationId)

    type MigrationHistoryDeps = 
        { ensureHistoryTable :  IDbCommand -> unit 
          tryFindLatest : IDbCommand -> MigrationId option }
    type MigrationRepositoryDeps =
        { tryFindByName : MigrationRepositoryDirectory -> MigrationEntryName ->  MigrationId option 
          tryFindLatest : MigrationRepositoryDirectory -> MigrationId option 
          getMigrationFiles : MigrationRepositoryDirectory -> DbUpdateAction -> MigrationFile seq }
    type UpdateDatabase = MigrationRepositoryDirectory -> MigrationEntryName option -> Result<string, string>
    type ExecuteMigrationScripts = IDbCommand -> MigrationFile seq -> unit

    let updateDatabase : IDbCommand 
                      -> MigrationHistoryDeps
                      -> MigrationRepositoryDeps
                      -> ExecuteMigrationScripts 
                      -> UpdateDatabase =
        fun dbCommand migrationHistory migrationRepository executeMigrationScripts migrationRepositoryDirectory entryName -> 
        let (<!>) = Option.map
        let (<*>) = Option.apply
        do migrationHistory.ensureHistoryTable dbCommand

        let latestIdFromHistory = migrationHistory.tryFindLatest dbCommand
        let latestIdFromRepository = migrationRepository.tryFindLatest migrationRepositoryDirectory
        let targetMigrationId = migrationRepository.tryFindByName 
                                <!> Some migrationRepositoryDirectory 
                                <*> entryName
                                |> Option.bind id

        getDbUpdateAction latestIdFromHistory latestIdFromRepository targetMigrationId 
        |> Result.map (migrationRepository.getMigrationFiles migrationRepositoryDirectory)
        |> Result.map (executeMigrationScripts dbCommand)
        |> Result.map (fun _ -> "Database is successfully updated")

    type TryFindLatestIdFromRepository = MigrationRepositoryDirectory -> MigrationId option
    type IsMigrationApplied = MigrationId -> bool
    type DeleteMigrationFileById = MigrationRepositoryDirectory -> MigrationId -> unit
    type RemoveMigration = MigrationRepositoryDirectory -> Result<string, string>

    let remove : TryFindLatestIdFromRepository 
              -> IsMigrationApplied
              -> DeleteMigrationFileById
              -> RemoveMigration =
        fun tryFindLatestIdFromRepository isMigrationApplied deleteMigrationFileById migrationRepositoryDirectory -> 
        let (<!>) = Result.map
        let (<*>) = Result.apply
        let latestIdFromRepository = tryFindLatestIdFromRepository migrationRepositoryDirectory
                                    |> Option.asResult "No migration entries exists"
        let deleteMigrationFile = fun isApplied ->
            if isApplied then Error "Migration entry is currently applied."
            else deleteMigrationFileById 
                <!> Ok migrationRepositoryDirectory 
                <*> latestIdFromRepository

        isMigrationApplied <!> latestIdFromRepository
        |> Result.bind deleteMigrationFile
        |> Result.map (fun _ -> "Migration entry successfully deleted")

    type ProcessCommandDeps = { initializeMigration : InitializeMigration
                                addMigration : AddMigration
                                updateDatabase : UpdateDatabase
                                removeMigration : RemoveMigration }

    let processCommand : ProcessCommandDeps
                      -> MigrationRepositoryState 
                      -> ParsedCommand
                      -> Result<string, string> =
        fun command migrationRepositoryState parsedCommand ->
        match parsedCommand, migrationRepositoryState with 
        | InitCommand, Exist _ -> Error "A repository already exists"
        | InitCommand, DoesNotExist repository -> command.initializeMigration repository
        | _, DoesNotExist _ -> Error "Migration repository does not exist"
        | _, Exist repository ->
            match parsedCommand with
            | AddCommand entryName -> command.addMigration repository entryName
            | UpdateCommand entryName -> command.updateDatabase repository entryName
            | RemoveCommand -> command.removeMigration repository
            | _ -> failwith "Command cannot be processed"