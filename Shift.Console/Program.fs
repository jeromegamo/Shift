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
            match o with
            | Initialize migrationRepositoryName -> 
                let ( <!> ) = Result.map
                let ( <*> ) = Result.apply
                initializeHandler <!> projectDirectory <*> (Ok migrationRepositoryName)
                |> Result.bind id
                |> function
                | Error e -> e
                | Ok o -> "Migration repository folder has been created"
            | _ -> failwith "Invalid operation"
            

    let run () =
        Console.ReadLine()
        |> executeCommand
        |> printfn "%s"

    [<EntryPoint>]
    let main argv =
        run()
        0
