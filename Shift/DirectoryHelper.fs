namespace Shift

module DirectoryHelper =

    open System
    open System.IO
    open System.Reflection
    open Shift.Common

    let rec private tryFindParent (name:string) (dir:DirectoryInfo) =
        match dir.Parent with
        | null -> None
        | dir when dir.Name = name ->
            { DirInfo.Name = dir.Name
              FullPath = dir.FullName } |> Some
        | _ -> tryFindParent name dir.Parent

    let getProjectDirectory : ProjectDirectoryName -> ProjectDirectory option =
        fun name ->
        let dir = DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)
        tryFindParent name dir
        
    let getMigrationRepositoryDir : MigrationRepositoryName -> ProjectDirectory -> MigrationRepositoryDirectory option =
        fun repoName projectDir ->
            let repositoryPath = Path.Combine(projectDir.FullPath, repoName)
            if Directory.Exists repositoryPath 
            then {DirInfo.Name = repoName
                  FullPath = repositoryPath} |> Some
            else None

    let getProjectName : unit -> ProjectDirectoryName =
        fun () -> Assembly.GetCallingAssembly().GetName().Name
