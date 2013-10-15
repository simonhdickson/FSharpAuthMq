namespace Host

open System
open System.Text
open ZeroMQ
open Serializer.Json

module ServiceHost =
    let private start serialize deserialize requestProcessor (receiveSocket:ZmqSocket) =
        async {
            while true do
                let request = receiveSocket.Receive Encoding.UTF8 |> deserialize
                let! response = requestProcessor request
                receiveSocket.Send(response |> serialize, Encoding.UTF8) |> ignore
        }

    let initialize () =
        ZmqContext.Create()
        |> Zmq.RequestResponse.responder "tcp://*:5556"
        |> start (serialize [||]) (deserialize [||]) Service.Authentication.isAuthorized
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        initialize ()
        Console.ReadLine() |> ignore
        0