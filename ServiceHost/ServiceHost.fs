namespace Host

open System
open System.Text
open ZeroMQ
open Serializer.Json

module ServiceHost =
    let private handlingPipeline = Service.Authentication.isAuthorized

    let private routeMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) _ =
        let message = routerFrom.ReceiveMessage()
        let frame = message.Unwrap ()
        routerTo.SendMessage(message) |> ignore

    let private routeBack (routerFrom:ZmqSocket) (routerTo:ZmqSocket) _ =
        let message = routerFrom.ReceiveMessage()
        routerTo.SendMessage(message) |> ignore

    let private start serialize deserialize ((backSocket, frontSocket):ZmqSocket*ZmqSocket) =
        async {
            frontSocket.ReceiveReady.Add (routeMessages frontSocket backSocket)
            backSocket.ReceiveReady.Add (routeBack backSocket frontSocket)
            let poller = new Poller([|frontSocket; backSocket|])
            while true do
                poller.Poll() |> ignore
        }

    let initialize () =
        ZmqContext.Create()
        |> Zmq.Router.router "tcp://*:5556" "tcp://*:5557"
        |> start (serialize [||]) (deserialize [||])
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        initialize ()
        Console.ReadLine() |> ignore
        0