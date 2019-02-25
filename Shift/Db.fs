namespace Shift

open System.Data
open System.Data.SqlClient
open Microsoft.Extensions.Configuration
open Shift

[<AutoOpen>]
module Db =

    type RawScript = string
    type ConnectionString = string

    let getConnectionString : ProjectDirectory -> string option =
        fun projectDirectory ->
        let builder = ConfigurationBuilder()
                        .SetBasePath(projectDirectory.FullPath)
                        .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
        let configuration = builder.Build()
        let connectionString = configuration.GetConnectionString("appDbConnection")
        if connectionString = null then None
        else Some connectionString

    let executeNonQuery : IDbCommand -> RawScript -> unit =
        fun cmd script ->
        cmd.CommandText <- script
        cmd.ExecuteNonQuery() |> ignore

    let executeScalar<'a> : IDbCommand -> 'a option=
        fun cmd -> 
        match cmd.ExecuteScalar() with
        | :? 'a as value -> Some value
        | _ -> None

    type GetDbTransaction = SqlConnection -> SqlTransaction option
    let executeDbScript : ConnectionString -> GetDbTransaction -> (SqlCommand -> 'r) -> 'r =
        fun connectionString getTransaction execute ->
        use connection = new SqlConnection(connectionString)
        connection.Open()
        let tx = getTransaction connection
        let cmd = connection.CreateCommand()
        match tx with
        | None ->
            try execute cmd 
            with _ -> reraise()
        | Some tx ->
            cmd.Transaction <- tx
            try
                let result = execute cmd
                tx.Commit()
                connection.Close()
                result
            with _ -> 
                tx.Rollback()
                connection.Close()
                reraise()

    let getDbTransaction = fun (conn:SqlConnection) -> Some (conn.BeginTransaction())
    let ignoreDbTransaction = fun _ -> None