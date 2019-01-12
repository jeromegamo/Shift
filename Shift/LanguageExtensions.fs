namespace Shift

[<AutoOpen>]
module LanguageExtensions = 

    type Option<'T> with

        static member apply f t =
            match f, t with
            | Some f, Some t -> Some (f t)
            | _ -> None

        static member asResult error option =
            match option with
            | None -> Error error
            | Some v -> Ok v
        
    type Result<'Ok, 'Error> with

        static member apply f t =
            match f, t with
            | Ok f, Ok t -> Ok (f t)
            | Ok f, Error e -> Error e
            | Error e, _ -> Error e
            

