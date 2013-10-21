namespace Container

open System
open System.Collections.Generic
open System.Text
open ZeroMQ
open Container.Pipeline
open Service.Authentication
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Serializer.Json
open Zmq.RequestResponse

module Service =
    let handlingPipeline pipelineState =
        pipelineState 
        |>  Pipeline.start
        >>= Service.Authentication.authenticate
        >>= Service.Authentication.revoke

    let rec private startService serialize deserialize (socket:ZmqSocket) =
        async {
            try
                socket.Linger <- System.TimeSpan.FromSeconds 0.1
                let request = socket.Receive Encoding.UTF8
                match request with  
                | null  -> match socket.ReceiveStatus with
                           | ReceiveStatus.Interrupted -> return ()
                           | status                    -> printfn "%A" status
                                                          do! startService serialize deserialize socket
                | _     -> let deserializedRequest = deserialize request
                           let! response = handlingPipeline ({ request=deserializedRequest; environment=Map.empty })
                           let (serializedResponse:string) =
                               match response with
                               | Handled s -> Success (s.environment.Item "result")|> serialize 
                               | _         -> Failure |> serialize
                           socket.Send(serializedResponse, Encoding.UTF8) |> ignore
                           do! startService serialize deserialize socket
            with
            | e -> printfn "%A" e
                   do! startService serialize deserialize socket
        }

    let initializeService (context:ZmqContext) endpoint =
        let converters : JsonConverter[] = [|UnionConverter<Result<Object>> (); UnionConverter<Command> ()|]
        
        Zmq.RequestResponse.responder endpoint context
        |> startService (serialize converters) (deserialize converters)
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        use context = ZmqContext.Create()
        initializeService context "tcp://*:5556"
        Console.ReadLine() |> ignore
        0