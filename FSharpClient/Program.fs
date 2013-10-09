open System
open System.Threading
open System.Text
open ZeroMQ

module Zmq =
    let requester addresss (zmqContext:ZmqContext) =
        let requester = zmqContext.CreateSocket(SocketType.REQ)
        requester.Connect addresss
        requester

module Client =
    let sendRequest (requestSocket:ZmqSocket) request =
        async {
            requestSocket.Send(request, Encoding.UTF8) |> ignore
            return requestSocket.Receive Encoding.UTF8
        }

let requests =
    [|("Bob",1); ("Bob",2); ("Alice",1)|]

[<EntryPoint>]
let main argv = 
    Thread.Sleep 5000
    use context = ZmqContext.Create()
    for i in 1..30  do
        async {
            let authorizationRequest =
                context
                |> Zmq.requester "tcp://localhost:5556"
                |> Client.sendRequest
            for j in 1..1000  do
                for (name,id) in requests do
                    let! isAuthorized =
                        authorizationRequest (sprintf "%s,%i" name id)
                    printfn "%s is authorized for %i %s" name id isAuthorized
                Thread.Sleep 5000
        } |> Async.Start

    Console.ReadLine() |> ignore
    0
