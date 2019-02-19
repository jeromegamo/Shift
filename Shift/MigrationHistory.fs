namespace Shift

open System.Data

module MigrationHistory =

    let tryFindLatest : IDbCommand -> MigrationId option =
        fun cmd ->
        let rawScript = @"select top 1 MigrationId from ShiftMigrationHistory
                          order by MigrationId desc"
        cmd.CommandText <- rawScript
        executeScalar<string> cmd
        |> Option.bind MigrationId.parse

    let ensureMigrationHistoryTable : IDbCommand -> unit =
        fun cmd ->
        let rawScript = @"if object_id('ShiftMigrationHistory') is null
                            create table ShiftMigrationHistory(
                                MigrationId varchar(114),
                                constraint PK_MigrationId primary key (MigrationId)
                          )"
        executeNonQuery cmd rawScript |> ignore

    let isMigrationApplied : IDbCommand -> MigrationId -> bool =
        fun cmd mId ->
        let rawScript = sprintf "select case when exists (
                                        select *
                                        from [ShiftMigrationHistory]
                                        where MigrationId = '%s'
                                    )
                                    then 'true'
                                    else 'false' end" <| mId.ToString()
        cmd.CommandText <- rawScript
        executeScalar<string> cmd
        |> function
        | Some "true" -> true
        | Some "false" -> false
        | _ -> false 