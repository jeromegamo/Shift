open System

let run () =
    Seq.initInfinite (fun _ -> Console.ReadLine())
    |> Seq.takeWhile (fun cmd -> cmd <> "exit")
    |> Seq.iter (fun cmd -> printfn "Input: %A" cmd)

[<EntryPoint>]
let main argv =
    run()
    printfn "Exited"
    0
