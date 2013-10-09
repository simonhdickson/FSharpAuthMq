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

[<EntryPoint>]
let main argv = 
    use context = ZmqContext.Create()
    for i in 1..100  do
        async {
            for (name,id) in  [|("Bob",1); ("Bob",2); ("Alice",1)|] do
                let authorizationRequest =
                    context
                    |> Zmq.requester "tcp://localhost:5556"
                    |> Client.sendRequest
                let! isAuthorized =
                    authorizationRequest (sprintf "%s,%i" name id)
                printfn "%s is authorized for %i %s" name id isAuthorized
        } |> Async.Start

    Console.ReadLine() |> ignore
    0
