namespace Shift.Console

module UpdateHandler =

    open System
    open System.IO
    open System.Data
    open System.Data.SqlClient
    open Shift
    open Shift.CommandHandler
    open Shift.MigrationRepository
    open Shift.MigrationHistory


    let readFile : MigrationFile -> string =
        fun file ->
        File.ReadAllText(file.File)

    let reducer accumulator current =
        accumulator + System.Environment.NewLine + current

    let createHistoryInsertionScript : MigrationFile seq -> string =
        fun files ->
        let init = @"insert into ShiftMigrationHistory(MigrationId)" + System.Environment.NewLine + "values "
        let values = files |> Seq.map (fun migration -> sprintf "('%s')" (migration.Id.ToString()))
                           |> String.concat ","
        init + values

    let mergeScripts : MigrationFile seq -> string =
        fun files -> 
        let migrationScript = files 
                              |> Seq.map readFile
                              |> Seq.reduce reducer
        let insertionScript = createHistoryInsertionScript files
        migrationScript + System.Environment.NewLine + insertionScript

    let executeMigrationScripts : IDbCommand -> MigrationFile seq -> unit =
        fun cmd files ->
        let script = mergeScripts files
        cmd.CommandText <- script
        cmd.ExecuteNonQuery() |> ignore

    let getMigrationScripts : MigrationRepositoryDirectory -> DatabaseSyncCommand -> MigrationFile seq =
        fun migrationRepoDir cmd ->
        match cmd with
        | (Update(from, upto)) -> getUpFilesByRange migrationRepoDir from upto
        | (Revert(from, upto)) -> getDownFilesByRange migrationRepoDir upto from
        | (UpdateFromFirst(target)) -> getUpFilesUntil migrationRepoDir target

    let updateController : ConnectionString -> MigrationRepositoryDirectory -> MigrationEntryName option -> Result<unit, UpdateDatabaseHandlerError> =
        fun connectionString migrationRepoDir name ->

        let transaction (dbcmd:IDbCommand) =
            ensureMigrationHistoryTable dbcmd
            let latestIdFromHistory = MigrationHistory.tryFindLatest dbcmd
            let latestIdFromRepository = MigrationRepository.tryFindLatest migrationRepoDir
            let targetMigrationId = name |> Option.map (MigrationRepository.tryFindByName migrationRepoDir)
                                         |> Option.bind id
            CommandHandler.updateDatabaseHandler latestIdFromHistory latestIdFromRepository targetMigrationId
            |> Result.map (getMigrationScripts migrationRepoDir)
            |> Result.map (executeMigrationScripts dbcmd)

        runTransactional connectionString transaction

