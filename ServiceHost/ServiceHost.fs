namespace Host

open System
open System.Text
open ZeroMQ
open Serializer.Json

module ServiceHost =
    let start serialize deserialize requestProcessor (recieveSocket:ZmqSocket) =
        async {
            while true do
                let request =
                    recieveSocket.Receive Encoding.UTF8 |> deserialize
                let response = requestProcessor request
                recieveSocket.Send(response |> serialize, Encoding.UTF8) |> ignore
        }

    [<EntryPoint>]
    let main argv =
        ZmqContext.Create()
        |> Zmq.RequestResponse.responder "tcp://*:5556"
        |> start (serialize [||]) (deserialize [||]) Service.Authentication.isAuthorized
        |> Async.Start
    
        Console.WriteLine(ZmqVersion.Current.ToString()) |> ignore

        Console.ReadLine() |> ignore
        0