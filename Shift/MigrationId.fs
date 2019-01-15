namespace Shift

open System
open System.Text.RegularExpressions
open Shift.Common

[<AutoOpen>]
module MigrationId =

    let MigrationIdPattern = Regex("(?<timestamp>[0-9]{14})-(?<name>[a-zA-Z]+)$", RegexOptions.Compiled)

    let [<Literal>] DateTimeStringFormat = "yyyyMMddhhmmss"

    [<CustomEquality; CustomComparison>]
    type MigrationId = private {name:string; timestamp:DateTime} with 

        member x.Name = x.name

        member x.TimeStamp = x.timestamp

        override x.ToString() = sprintf "%s-%s" (x.timestamp.ToString(DateTimeStringFormat)) x.name

        override left.Equals right =
            match right with
            | :? MigrationId as right -> (left.Name = right.Name) && (left.TimeStamp = right.TimeStamp)
            | _ -> false

        override left.GetHashCode () = hash left.TimeStamp

        interface IComparable with
            member left.CompareTo right =
                match right with
                | :? MigrationId as right -> compare left.TimeStamp right.TimeStamp
                | _ -> invalidArg "right" "cannot compare values with different type"

    type MigrationIdError =
        | Required
        | MaxLength
        | MinLength
    
    type Name = string

    let create : TimeStamp -> Name -> Result<MigrationId, MigrationIdError> =
        fun timestamp name ->
            let name = name.Trim()
            match name with
            | n when n |> String.length = 0 -> Error  Required
            | n when n |> String.length < 4 -> Error MinLength
            | n when n |> String.length > 100 -> Error MaxLength
            | n -> Ok {name=name; timestamp= timestamp }

    let parse value = 
        let toDateTime value = DateTime.ParseExact(value, DateTimeStringFormat, null)
        let m = MigrationIdPattern.Match value
        if m.Success then Some { name = m.Groups.["name"].Value;
                                 timestamp = m.Groups.["timestamp"].Value |> toDateTime }
        else None
