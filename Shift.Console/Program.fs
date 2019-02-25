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
    let updateDatabase' : ConnectionString -> MigrationRepositoryDirectory -> MigrationEntryName option -> Result<string, string> =
        fun connectionString migrationRepositoryDirectory migrationEntryName ->

        let transaction = fun dbcmd ->
            let migrationHistoryDeps =
                { tryFindLatest = fun _ -> MigrationHistory.tryFindLatest dbcmd }
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

        Db.executeDbScript connectionString Db.getDbTransaction transaction

    let removeMigration' : ConnectionString -> MigrationRepositoryDirectory -> Result<string, string> =
        fun connectionString migrationRepositoryDirectory ->

        let transaction = fun dbcmd ->
            Command.remove MigrationRepository.tryFindLatest
                           (MigrationHistory.isMigrationApplied dbcmd)
                           MigrationRepository.deleteById
                           migrationRepositoryDirectory

        Db.executeDbScript connectionString Db.getDbTransaction transaction

    let getConnectionString' : ProjectDirectory -> unit -> ConnectionString option =
        fun projectDirectory _ ->
        Common.getConnectionString projectDirectory

    let ensureMigrationHistoryTable' : ConnectionString -> unit =
        fun connectionString ->
        Db.executeDbScript connectionString Db.getDbTransaction MigrationHistory.ensureMigrationHistoryTable

    let getProcessDeps : InitializeMigration 
                      -> AddMigration 
                      -> (ConnectionString -> UpdateDatabase)
                      -> (ConnectionString -> RemoveMigration)
                      -> ProcessCommandDeps =
        fun initializeMigration addMigration updateDatabase removeMigration -> 
        { initializeMigration = initializeMigration
          addMigration = addMigration
          updateDatabase = updateDatabase
          removeMigration = removeMigration  }

    let (<!>) = Result.map
    let (<*>) = Result.apply

    let run = fun _ ->
        Console.ReadLine()
        |> CommandParser.parseCommand
        |> function
        | Error e -> printfn "%s" e
        | Ok parsedCommand ->
            let migrationRepositoryName = "ShiftMigrations"
            let name = DirectoryHelper.getProjectName()
            let projectDirectory = DirectoryHelper.getProjectDirectory name
                                |> Option.asResult "Project directory does not exist"
            let migrationRepositoryState = getMigrationRepositoryState 
                                        <!> (Ok Directory.Exists) 
                                        <*> (Ok migrationRepositoryName)
                                        <*> projectDirectory
            let connectionString = getConnectionString' <!> projectDirectory
            let processCommandDeps = getProcessDeps 
                                    <!> (Ok Command.initializeMigration)
                                    <*> (Ok addMigration')
                                    <*> (Ok updateDatabase')
                                    <*> (Ok removeMigration')
            Command.processCommand 
                <!> connectionString
                <*> processCommandDeps
                <*> (Ok ensureMigrationHistoryTable')
                <*> migrationRepositoryState
                <*> (Ok parsedCommand)
                |> Result.bind id
                |> function
                | Ok o -> printfn "%s" o
                | Error e -> printfn "%s" e

    [<EntryPoint>]
    let main argv =
        run()
        0
