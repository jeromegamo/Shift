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

            
