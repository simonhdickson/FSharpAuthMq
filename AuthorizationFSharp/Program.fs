open System
open System.Text
open ZeroMQ
open Server

module AuthorizationService =
    type Request = { name:string; id:int }

    let isAuthorized request =
        async {
            if request.name = "Bob" && request.id = 1 then
                return true
            else
                return false
        }

[<EntryPoint>]
let main argv =
    ZmqContext.Create()
    |> Zmq.responder "tcp://*:5556"
    |> Server.start (Json.convertFrom [||]) (Json.convertTo [||]) AuthorizationService.isAuthorized
    |> Async.Start
    
    Console.WriteLine(ZmqVersion.Current.ToString()) |> ignore

    Console.ReadLine() |> ignore
    0
