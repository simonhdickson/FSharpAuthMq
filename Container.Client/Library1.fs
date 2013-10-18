open System
open ZeroMQ
open Client.Authentication
open Zmq.RequestResponse

[<EntryPoint>]
let main argv =
    use context = ZmqContext.Create ()
    let response = authenticate context "jbloggs" "letmein"
    match response with
    | Success ok -> printfn "%b" ok
    | Timeout -> printfn "Need science yo"
    | Failure -> printfn "RUN!"
    Console.ReadLine() |> ignore
    0  