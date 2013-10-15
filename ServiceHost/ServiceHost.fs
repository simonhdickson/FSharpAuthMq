namespace Host

open System
open System.Collections.Generic
open System.Text
open ZeroMQ
open Serializer.Json

module ServiceHost =
    let private routeMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
        let message = routerFrom.ReceiveMessage()
        let identity = message.Unwrap()
        let messageType = Encoding.UTF8.GetString(message.Unwrap().Buffer)
        message.Wrap(identity)
        message.Wrap(queue.Dequeue())
        routerTo.SendMessage(message) |> ignore

    let private routeBack (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
        let message = routerFrom.ReceiveMessage()
        let identity = message.Unwrap()
        queue.Enqueue(identity)
        let address = Encoding.UTF8.GetString(message.[0].Buffer)
        if (address <> "Ready yo") then
            routerTo.SendMessage(message) |> ignore

    let private startRouter serialize deserialize ((frontSocket, backSocket):ZmqSocket*ZmqSocket) =
        async {
            let queue = new Queue<Frame>()
            frontSocket.ReceiveReady.Add (routeMessages frontSocket backSocket queue)
            backSocket.ReceiveReady.Add (routeBack backSocket frontSocket queue)
            let poller = new Poller([|frontSocket; backSocket|])
            while true do
                poller.Poll() |> ignore
        }

    let private startAuthenticator serialize deserialize handlingPipeline (socket:ZmqSocket) =
        async {
            socket.Send("Ready yo", Encoding.UTF8) |> ignore
            while true do
                let message = socket.ReceiveMessage()
                let clientAddress = message.Unwrap()
                let request = Encoding.UTF8.GetString(message.Unwrap().Buffer)
                let deserializedRequest = deserialize request
                let! response = handlingPipeline deserializedRequest
                let (serializedResponse:string) = serialize response
                let responseMessage = new ZmqMessage ([|Encoding.UTF8.GetBytes(serializedResponse)|])
                responseMessage.Wrap(clientAddress)
                socket.SendMessage(responseMessage) |> ignore
        }

    let initializeRouter (context:ZmqContext) =
        context
        |> Zmq.Router.router "tcp://*:5556" "inproc://Authenticate"
        |> startRouter (serialize [||]) (deserialize [||])
        |> Async.Start

    let initializeAuthenticator (context:ZmqContext) =
        context
        |> Zmq.RequestResponse.requester "inproc://Authenticate"
        |> startAuthenticator (serialize [||]) (deserialize [||]) Service.Authentication.authenticate
        |> Async.Start

    let initializeRevoker (context:ZmqContext) =
        context
        |> Zmq.RequestResponse.requester "inproc://RevokeUser"
        |> startAuthenticator (serialize [||]) (deserialize [||]) Service.Authentication.revoke
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        use context = ZmqContext.Create()
        initializeRouter context
        initializeAuthenticator context
        Console.ReadLine() |> ignore
        0