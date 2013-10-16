namespace Host

open System
open System.Collections.Generic
open System.Text
open ZeroMQ
open ServiceHost.Pipeline
open ServiceHost.Pipeline.PipelineExecution
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Serializer.Json

module ServiceHost =
//    let private routeMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
//        let message = routerFrom.ReceiveMessage()
//        let identity = message.Unwrap()
//        let messageType = Encoding.UTF8.GetString(message.Unwrap().Buffer)
//        message.Wrap(identity)
//        message.Wrap(queue.Dequeue())
//        routerTo.SendMessage(message) |> ignore
//
//    let private routeBack (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
//        let message = routerFrom.ReceiveMessage()
//        let identity = message.Unwrap()
//        queue.Enqueue(identity)
//        let address = Encoding.UTF8.GetString(message.[0].Buffer)
//        if (address <> "Ready yo") then
//            routerTo.SendMessage(message) |> ignore
//
//    let private startRouter serialize deserialize ((frontSocket, backSocket):ZmqSocket*ZmqSocket) =
//        async {
//            let queue = new Queue<Frame>()
//            frontSocket.ReceiveReady.Add (routeMessages frontSocket backSocket queue)
//            backSocket.ReceiveReady.Add (routeBack backSocket frontSocket queue)
//            let poller = new Poller([|frontSocket; backSocket|])
//            while true do
//                poller.Poll() |> ignore
//        }
//
//    let private startAuthenticator serialize deserialize handlingPipeline (socket:ZmqSocket) =
//        async {
//            socket.Send("Ready yo", Encoding.UTF8) |> ignore
//            while true do
//                let message = socket.ReceiveMessage()
//                let clientAddress = message.Unwrap()
//                let request = Encoding.UTF8.GetString(message.Unwrap().Buffer)
//                let deserializedRequest = deserialize request
//                let! response = handlingPipeline deserializedRequest
//                let (serializedResponse:string) = serialize response
//                let responseMessage = new ZmqMessage ([|Encoding.UTF8.GetBytes(serializedResponse)|])
//                responseMessage.Wrap(clientAddress)
//                socket.SendMessage(responseMessage) |> ignore
//        }

//    let initializeRouter (context:ZmqContext) =
//        context
//        |> Zmq.Router.router "tcp://*:5556" "inproc://Authenticate"
//        |> startRouter (serialize [||]) (deserialize [||])
//        |> Async.Start
//
//    let initializeAuthenticator (context:ZmqContext) =
//        context
//        |> Zmq.RequestResponse.requester "inproc://Authenticate"
//        |> startAuthenticator (serialize [||]) (deserialize [||]) Service.Authentication.authenticate
//        |> Async.Start
//
//    let initializeRevoker (context:ZmqContext) =
//        context
//        |> Zmq.RequestResponse.requester "inproc://RevokeUser"
//        |> startAuthenticator (serialize [||]) (deserialize [||]) Service.Authentication.revoke
//        |> Async.Start

    let handlingPipeline pipelineState =
        async { return Continue pipelineState }
        |> bind Service.Authentication.authenticate
        |> bind Service.Authentication.revoke

    let private startService serialize deserialize (socket:ZmqSocket) =
        async {
            try
                while true do
                    let message = socket.ReceiveMessage()
                    let request = Encoding.UTF8.GetString(message.Unwrap().Buffer)
                    let deserializedRequest = deserialize request
                    let! response = handlingPipeline ({ request=deserializedRequest; environment=Map.empty })
                    let (serializedResponse:string) =
                        match response with
                        | Handled s     -> serialize (s.environment.Item "result")
                        | Continue s    -> "fail - no science"
                        | Abort a       -> a.errorMessage
                    let responseMessage = new ZmqMessage ([|Encoding.UTF8.GetBytes(serializedResponse)|])
                    socket.SendMessage(responseMessage) |> ignore
            with
            | e -> printfn "%A" e
        }

    let initializeService (context:ZmqContext) =
        let converters : JsonConverter[] = [|UnionConverter<Service.Authentication.Command> ()|]
        context
        |> Zmq.RequestResponse.responder "tcp://*:5556"
        |> startService (serialize [||]) (deserialize converters)
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        use context = ZmqContext.Create()
//        initializeRouter context
//        initializeAuthenticator context
        initializeService context
        Console.ReadLine() |> ignore
        0