namespace Shift

module Program =

    open System
    open System.Reflection
    open Shift
    open Shift.CommandParser
    open Shift.DirectoryHelper
    open Shift.CommandHandler

    // type ApplicationError = string

    let executeCommand : RawCommand -> string =
        fun rawcmd -> 
        let defaultMigrationRepositoryName = "ShiftMigrations"
        let name = Assembly.GetExecutingAssembly().GetName().Name

        let projectDirectory = DirectoryHelper.getProjectDirectory name
                               |> Option.asResult "Project not found"

        parseCommand defaultMigrationRepositoryName rawcmd
        |> function
        | Error e -> e
        | Ok o -> 
            let ( <!> ) = Result.map
            let ( <*> ) = Result.apply
            match o with
            | Initialize migrationRepositoryName -> 
                initializeHandler <!> projectDirectory <*> (Ok migrationRepositoryName)
                |> Result.bind id
                |> function
                | Error e -> e
                | Ok o -> "Migration repository folder has been created"
            | AddMigration migrationEntryName ->
                let timestamp = DateTime.UtcNow
                let migrationRepositoryDir = 
                    DirectoryHelper.getMigrationRepositoryDir 
                    <!> (Ok defaultMigrationRepositoryName)
                    <*> projectDirectory

                let migrationRepositoryDir' = 
                    migrationRepositoryDir
                    |> function
                    | Error e -> Error e
                    | Ok (None) -> Error "Migration repository does not exist"
                    | Ok (Some repodir) -> Ok repodir

                addMigrationHandler <!> (Ok timestamp) <*> migrationRepositoryDir' <*> (Ok migrationEntryName)
                |> function
                | Error e -> e
                | Ok o -> sprintf "Migration entry created %A" o
            | _ -> failwith "Invalid operation"
            

    let run () =
        Console.ReadLine()
        |> executeCommand
        |> printfn "%s"

    [<EntryPoint>]
    let main argv =
        run()
        0
