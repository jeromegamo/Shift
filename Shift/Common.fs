namespace Shift

[<AutoOpen>]
module Common =

    open System
    open Microsoft.Extensions.Configuration

    type DirInfo =
        { Name : string 
          FullPath : string }

    type ProjectDirectory = DirInfo

    type MigrationRepositoryDirectory = DirInfo

    type ProjectDirectoryName = string

    type MigrationRepositoryName = string

    type MigrationEntryName = string

    type TimeStamp = DateTime

    type GetTimeStamp = unit -> TimeStamp

    let getConnectionString : ProjectDirectory -> string option =
        fun projectDirectory ->
        let builder = ConfigurationBuilder()
                        .SetBasePath(projectDirectory.FullPath)
                        .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
        let configuration = builder.Build()
        let connectionString = configuration.GetConnectionString("appDbConnection")
        if connectionString = null then None
        else Some connectionString