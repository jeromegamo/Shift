namespace Shift

open System
open System.IO
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

    let processCommand : InitializeMigration 
                      -> AddMigration
                      -> MigrationRepositoryState 
                      -> ParsedCommand
                      -> Result<string, string> =
        fun initializeMigration addMigration migrationRepositoryState parsedCommand ->
        match parsedCommand, migrationRepositoryState with 
        | InitCommand, Exist _ -> Error "A repository already exists"
        | InitCommand, DoesNotExist repository -> initializeMigration repository
        | _, DoesNotExist _ -> Error "Migration repository does not exist"
        | _, Exist repository ->
            match parsedCommand with
            | AddCommand entryName -> addMigration repository entryName
            | UpdateCommand entryName -> Ok ""
            | RemoveCommand -> Ok ""
            | _ -> failwith "Command cannot be processed"