namespace Shift

module Common =

    open System

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
