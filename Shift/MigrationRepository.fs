namespace Shift

module MigrationRepository =

    open System.IO
    open Shift
    open Shift.Common

    type MigrationEntry = 
        { Id : MigrationId
          Directory : DirInfo }

    let private createMigrationEntryFiles : MigrationEntry -> unit =
        fun entry ->
        let createFile name =
            let file = Path.Combine(entry.Directory.FullPath, name)
            File.Create file |> ignore
        ["Up.sql"; "Down.sql"] |> Seq.iter createFile

    let createMigrationEntry : MigrationRepositoryDirectory 
                            -> MigrationId 
                            -> unit = 
        fun repo mId -> 
        let entryPath = Path.Combine(repo.FullPath, mId.ToString())
        let createdEntry = Directory.CreateDirectory(entryPath)
        if createdEntry.Exists 
        then let dirInfo = { DirInfo.Name = mId.ToString() 
                             FullPath = entryPath }
             let entry = { Id = mId; 
                           Directory =  dirInfo }
             createMigrationEntryFiles entry
        else failwith "Migration entry not created"