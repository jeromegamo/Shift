namespace Shift

module CommandHandler =

    open System.IO
    open Shift.Common

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