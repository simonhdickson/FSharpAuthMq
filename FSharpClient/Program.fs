open System
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

[<EntryPoint>]
let main argv = 
    let authorizationRequest =
        ZmqContext.Create()
        |> Zmq.requester "tcp://localhost:5556"
        |> Client.sendRequest

    async {
        for (name,id) in  [|("Bob",1); ("Bob",2); ("Alice",1)|] do
            let request = sprintf "%s,%i" name id
            let! isAuthorized = authorizationRequest request
            printfn "%s is authorized for id %i: %s" name id isAuthorized
    } |> Async.Start

    Console.ReadLine() |> ignore
    0
