namespace Shift.Console

module Program =

    open System
    open System.Reflection
    open System.IO
    open System.Data
    open Shift
    open Shift.CommandParser
    open Shift.DirectoryHelper
    open Shift.MigrationRepository
    open Shift.Command

    let addMigration' : AddMigration =
        let getTimeStamp = fun unit -> DateTime.UtcNow
        Command.addMigration getTimeStamp
                                MigrationRepository.createMigrationEntry
    let updateDatabaseSetup : IDbCommand -> MigrationRepositoryDirectory -> MigrationEntryName option -> Result<string, string> =
        fun dbcmd migrationRepositoryDirectory migrationEntryName ->

        let migrationHistoryDeps =
            { ensureHistoryTable = fun _ -> MigrationHistory.ensureMigrationHistoryTable dbcmd 
              tryFindLatest = fun _ -> MigrationHistory.tryFindLatest dbcmd }
        let migrationRepositoryDeps = 
            { tryFindByName = MigrationRepository.tryFindByName
              tryFindLatest = MigrationRepository.tryFindLatest
              getUpFilesByRange =  MigrationRepository.getUpFilesByRange
              getDownFilesByRange = MigrationRepository.getDownFilesByRange
              getUpFilesUntil = MigrationRepository.getUpFilesUntil }
        
        let executeMigrationScripts = Command.executeMigrationScripts dbcmd

        Command.updateDatabase 
            migrationHistoryDeps
            migrationRepositoryDeps
            executeMigrationScripts
            migrationRepositoryDirectory
            migrationEntryName

    let removeMigrationSetup : IDbCommand -> MigrationRepositoryDirectory -> Result<string, string> =
        fun dbcmd migrationRepositoryDirectory ->

        Command.remove MigrationRepository.tryFindLatest
                       (MigrationHistory.isMigrationApplied dbcmd)
                       MigrationRepository.deleteById
                       migrationRepositoryDirectory

    let getProcessDeps : InitializeMigration 
                      -> AddMigration 
                      -> UpdateDatabase 
                      -> RemoveMigration 
                      -> ProcessCommandDeps =
        fun initializeMigration addMigration updateDatabase removeMigration -> 
        { initializeMigration = initializeMigration
          addMigration = addMigration
          updateDatabase = updateDatabase
          removeMigration = removeMigration  }

    let (<!>) = Result.map
    let (<*>) = Result.apply

    let run = fun _ ->
        let parsedCommand = Console.ReadLine()
                            |> CommandParser.parseCommand
        let migrationRepositoryName = "ShiftMigrations"
        let name = DirectoryHelper.getProjectName()
        let projectDirectory = DirectoryHelper.getProjectDirectory name
                               |> Option.asResult "Project directory does not exist"
        let migrationRepositoryState = getMigrationRepositoryState 
                                    <!> (Ok Directory.Exists) 
                                    <*> (Ok migrationRepositoryName)
                                    <*> projectDirectory
        let connectionString = getConnectionString <!> projectDirectory
                               |> Result.bind (fun conn -> 
                                    match conn with 
                                    | None -> Error "Connection string does not exist"
                                    | Some a -> Ok a)
        let updateDatabase' = Db.executeDbScript 
                                <!> connectionString 
                                <*> (Ok Db.getDbTransaction) 
                                <*> (Ok updateDatabaseSetup)
        let removeMigration' = Db.executeDbScript 
                                <!> connectionString 
                                <*> (Ok Db.getDbTransaction) 
                                <*> (Ok removeMigrationSetup)
        let processCommandDeps = getProcessDeps 
                                   <!> (Ok Command.initializeMigration)
                                   <*> (Ok addMigration')
                                   <*> updateDatabase'
                                   <*> removeMigration'
        Command.processCommand 
            <!> processCommandDeps
            <*> migrationRepositoryState
            <*> parsedCommand
            |> Result.bind id
            |> function
            | Ok o -> printfn "%s" o
            | Error e -> printfn "%s" e

    [<EntryPoint>]
    let main argv =
        run()
        0
