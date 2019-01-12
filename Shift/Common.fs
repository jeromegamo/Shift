namespace Shift

module Common =
    
    type DirInfo =
        { Name : string 
          FullPath : string }

    type ProjectDirectory = DirInfo

    type MigrationRepositoryDirectory = DirInfo

    type ProjectDirectoryName = string

    type MigrationRepositoryName = string