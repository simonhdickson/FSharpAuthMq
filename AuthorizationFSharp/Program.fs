open System
open System.Text
open ZeroMQ

module Zmq =
    let responder addresss (zmqContext:ZmqContext) =
        let responder = zmqContext.CreateSocket SocketType.REP
        responder.Bind addresss
        responder

module Server =
    let start requestProcessor (recieveSocket:ZmqSocket) =
        async {
            while true do
                let request = recieveSocket.Receive Encoding.UTF8
                let! response = requestProcessor request
                recieveSocket.Send(response, Encoding.UTF8) |> ignore
        }

let isAuthorized (message:string) =
    async {
        printfn "Recieved message: %s" message
        let x = message.Split ','
        if x.[0] = "Bob" && x.[1] = "1" then
            return true.ToString()
        else
            return false.ToString()
    }

[<EntryPoint>]
let main argv =
    ZmqContext.Create()
    |> Zmq.responder "tcp://*:5556"
    |> Server.start isAuthorized
    |> Async.Start

    Console.ReadLine() |> ignore
    0
