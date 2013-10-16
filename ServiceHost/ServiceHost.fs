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
        initializeService context
        Console.ReadLine() |> ignore
        0