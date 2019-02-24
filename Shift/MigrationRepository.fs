namespace Shift

module MigrationRepository =

    open System
    open System.Linq
    open System.IO
    open Shift
    open Shift.Common

    type MigrationEntry = 
        { Id : MigrationId
          Directory : DirInfo }

    type MigrationFile =
        { Id:MigrationId
          File: string }
          
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

    let convertFileInfoToMigrationFile : FileInfo -> MigrationFile option =
        fun file ->
        MigrationId.parse file.DirectoryName 
        |> Option.map (fun migId -> 
                { MigrationFile.Id = migId
                  File = file.FullName })
    
    let convertFileInfoToMigrationId : FileInfo -> MigrationId option =
        fun file ->
        MigrationId.parse file.DirectoryName

    let convertDirectoryInfoToMigrationId (dir:DirectoryInfo) =
        MigrationId.parse dir.Name

    let getUpFilesByRange : MigrationRepositoryDirectory -> MigrationId -> MigrationId -> MigrationFile seq =
        fun dir start ``end`` ->
        let repo = DirectoryInfo dir.FullPath
        repo.EnumerateFiles("*Up.sql", SearchOption.AllDirectories)
        |> Seq.map convertFileInfoToMigrationFile
        |> Seq.choose id
        |> Seq.sortBy (fun file -> file.Id)
        |> Seq.filter (fun file -> file.Id > start)
        |> Seq.filter (fun file -> file.Id <= ``end``)
    
    let getDownFilesByRange : MigrationRepositoryDirectory -> MigrationId -> MigrationId -> MigrationFile seq =
        fun dir start ``end`` ->
        let repo = DirectoryInfo dir.FullPath
        repo.EnumerateFiles("*Down.sql", SearchOption.AllDirectories)
        |> Seq.map convertFileInfoToMigrationFile
        |> Seq.choose id
        |> Seq.sortByDescending (fun file -> file.Id)
        |> Seq.filter (fun file -> file.Id > start)
        |> Seq.filter (fun file -> file.Id <= ``end``)

    let getUpFilesUntil : MigrationRepositoryDirectory -> MigrationId -> MigrationFile seq =
        fun dir target ->
        let repo = DirectoryInfo dir.FullPath
        repo.EnumerateFiles("*Up.sql", SearchOption.AllDirectories)
        |> Seq.map convertFileInfoToMigrationFile
        |> Seq.choose id
        |> Seq.sortBy (fun file -> file.Id)
        |> Seq.filter (fun file -> file.Id <= target)

    let tryFindLatest : MigrationRepositoryDirectory -> MigrationId option =
        fun dir ->
        let repo = DirectoryInfo dir.FullPath
        repo.EnumerateFiles("*Up.sql", SearchOption.AllDirectories)
        |> Seq.map convertFileInfoToMigrationId
        |> Seq.choose id
        |> Seq.sortByDescending id
        |> Seq.tryHead

    let tryFindByName : MigrationRepositoryDirectory -> MigrationEntryName -> MigrationId option =
        fun dir name ->
        let repo = DirectoryInfo dir.FullPath
        repo.EnumerateDirectories()
        |> Seq.map convertDirectoryInfoToMigrationId
        |> Seq.choose id
        |> Seq.sortByDescending id
        |> Seq.tryFind (fun id -> id.Name.Contains(name))

    let deleteById : MigrationRepositoryDirectory -> MigrationId -> unit =
        fun dir mId ->
        let dirToDelete = Path.Combine(dir.FullPath, mId.ToString())
        Directory.Delete(dirToDelete,true) |> ignore

    type IsDirectoryExists = string -> bool

    type MigrationRepositoryState =
        | Exist of MigrationRepositoryDirectory
        | DoesNotExist of MigrationRepositoryDirectory

    let getMigrationRepositoryState : IsDirectoryExists -> MigrationRepositoryName -> ProjectDirectory -> MigrationRepositoryState =
        fun isDirectoryExists repositoryName projectDirectory ->
        let repositoryPath = Path.Combine(projectDirectory.FullPath, repositoryName)
        let dir = { Name = repositoryName
                    FullPath = repositoryPath } 
        if isDirectoryExists dir.FullPath 
        then Exist dir
        else DoesNotExist dir