namespace Shift

module DirectoryHelper =

    open System
    open System.IO
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


