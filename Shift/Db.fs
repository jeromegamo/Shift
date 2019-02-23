namespace Shift

open System.Data
open System.Data.SqlClient
open Shift

[<AutoOpen>]
module Db =

    type RawScript = string
    type ConnectionString = string

    let executeNonQuery : IDbCommand -> RawScript -> unit =
        fun cmd script ->
        cmd.CommandText <- script
        cmd.ExecuteNonQuery() |> ignore

    let executeScalar<'a> : IDbCommand -> 'a option=
        fun cmd -> 
        match cmd.ExecuteScalar() with
        | :? 'a as value -> Some value
        | _ -> None

    let runTransactional : ConnectionString ->  (IDbCommand -> 'a)  -> 'a=
        fun connectionString execute ->
        use con = new SqlConnection(connectionString)
        con.Open()
        let tx = con.BeginTransaction()
        let cmd = con.CreateCommand()
        cmd.Transaction <- tx
        try 
            let result = execute cmd
            tx.Commit()
            result
        with _ -> tx.Rollback()
                  reraise()

    let runNonTransactional : ConnectionString ->  (IDbCommand -> 'a)  -> 'a =
        fun connectionString execute ->
        use con = new SqlConnection(connectionString)
        con.Open()
        let cmd = con.CreateCommand()
        execute cmd
