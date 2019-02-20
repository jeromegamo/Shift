namespace Shift.Console

module Program =

    open System
    open System.Reflection
    open Shift
    open Shift.CommandParser
    open Shift.DirectoryHelper
    open Shift.CommandHandler

    let initializeHandler' : ProjectDirectory -> MigrationRepositoryDirectory option -> MigrationRepositoryName -> Result<string, string> =
        fun projectDirectory migrationRepositoryDirectory migrationRepositoryName ->
        let (<!>) = Result.map
        let (<*>) = Result.apply
        match migrationRepositoryDirectory with
        | None -> 
            initializeHandler <!> (Ok projectDirectory) <*> (Ok migrationRepositoryName)
            |> Result.bind id
            |> Result.map (fun _ -> "Migration repository folder has been created")
        | Some _ -> Error "There is an existing migration repository" 

    let addMigrationHandler' : ProjectDirectory -> MigrationRepositoryDirectory option -> MigrationEntryName -> Result<string, string> =
        fun projectDirectory migrationRepositoryDirectory migrationEntryName ->
        let (<!>) = Result.map
        let (<*>) = Result.apply
        match migrationRepositoryDirectory with
        | None -> Error "No migration repository exists"
        | Some dir ->
            let timestamp = DateTime.UtcNow 
            addMigrationHandler <!> (Ok timestamp) <*> (Ok dir) <*> (Ok migrationEntryName)
            |> Result.bind id
            |> function
            | Ok o -> Ok "Migration entry is created"
            | Error (MigrationIdError Required) -> Error "Migration name is required"
            | Error (MigrationIdError MaxLength) -> Error "Migration name is too long"
            | Error (MigrationIdError MinLength) -> Error "Migration name is too short"

    let updateCommandHandler' : ProjectDirectory -> MigrationRepositoryDirectory option -> MigrationEntryName option -> Result<string, string> =
        fun projectDirectory migrationRepositoryDirectory migrationEntryName -> 
        match migrationRepositoryDirectory with
        | None -> Error "No migration repository exists"
        | Some migrationRepositoryDirectory ->
            let (<!>) = Result.map
            let (<*>) = Result.apply
            let connectionString = Common.getConnectionString projectDirectory
                                   |> Option.asResult "Cannot find connection string"
            UpdateHandler.updateController <!> connectionString <*> (Ok migrationRepositoryDirectory) <*> (Ok migrationEntryName)
            |> function
            | Error e -> Error e
            | Ok o -> 
                match o with
                | Ok _ -> Ok "Database Updated"
                | Error NoMigrationEntries -> Error "No migration entries"
                | Error MissingMigrationEntries -> Error "Missing migration entries"
                | Error AlreadyUpToDate -> Error "Already up to date"

    let removeMigrationHandler : ConnectionString -> ProjectDirectory -> MigrationRepositoryDirectory option -> Result<string, string> =
        fun connectionString projectDirectory migrationRepositoryDirectory ->
        match migrationRepositoryDirectory with
        | None -> Error "No migration repository exists"
        | Some migrationRepositoryDirectory ->
            let migrationId = MigrationRepository.tryFindLatest migrationRepositoryDirectory
            match migrationId with
            | None -> Error "No Migration files exists"
            | Some mId ->
                let (<!>) = Result.map
                let (<*>) = Result.apply
                let isMigrationApplied' = Db.runTransactional 
                                        <!> (Ok connectionString)
                                        <*> (Ok (fun dbcmd -> 
                                                    MigrationHistory.ensureMigrationHistoryTable dbcmd
                                                    MigrationHistory.isMigrationApplied dbcmd mId))
                match isMigrationApplied' with
                | Error e -> Error e
                | Ok isApplied ->
                    if isApplied then Error "Migration entry is currently applied"
                    else MigrationRepository.deleteById migrationRepositoryDirectory mId
                         Ok "Migration entry removed"

    let run = 
        let (<!>) = Result.map
        let (<*>) = Result.apply
        let migrationRepositoryName = "ShiftMigrations"
        let name = DirectoryHelper.getProjectName()
        let projectDirectory = DirectoryHelper.getProjectDirectory name
                            |> Option.asResult "Project directory does not exist"
        let parsedCommand = Console.ReadLine()
                            |> CommandParser.parseCommand
        let migrationRepositoryState = getMigrationRepositoryState 
                                    <!> (Ok Directory.Exists) 
                                    <*> (Ok migrationRepositoryName)
                                    <*> projectDirectory
        let result = processCommand 
                    <!> (Ok initializeMigration)
                    <*> migrationRepositoryState 
                    <*> parsedCommand
                    |> Result.bind id

        match result with
        | Error e -> printfn "%A" e
        | Ok o -> printfn "%A" o

    [<EntryPoint>]
    let main argv =
        run()
        0
