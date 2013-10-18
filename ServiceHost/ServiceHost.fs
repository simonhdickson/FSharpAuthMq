namespace Host

open System
open System.Collections.Generic
open System.Text
open ZeroMQ
open ServiceHost.Pipeline
open ServiceHost.Pipeline.PipelineExecution
open Service.Authentication
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Serializer.Json
open Zmq.RequestResponse

module ServiceHost =
    let handlingPipeline pipelineState =
        async { return Continue pipelineState }
        ||> Service.Authentication.authenticate
        ||> Service.Authentication.revoke

    let rec private startService serialize deserialize killGuid (socket:ZmqSocket) =
        async {
            try
                socket.Linger <- System.TimeSpan.FromSeconds 0.1
                let request = socket.Receive Encoding.UTF8
                match request with  
                | null ->
                    match socket.ReceiveStatus with
                    | ReceiveStatus.Interrupted -> return ()
                    | status -> printfn "%A" status
                                do! startService serialize deserialize killGuid socket
                | _ -> let deserializedRequest = deserialize request
                       let! response = handlingPipeline ({ request=deserializedRequest; environment=Map.empty })
                       let (serializedResponse:string) =
                           match response with
                           | Handled s -> Success (s.environment.Item "result")|> serialize 
                           | _         -> Failure |> serialize
                       socket.Send(serializedResponse, Encoding.UTF8) |> ignore
                       do! startService serialize deserialize killGuid socket
            with
            | e -> printfn "%A" e
                   do! startService serialize deserialize killGuid socket
        }

    let initializeService (context:ZmqContext) endpoint =
        let killGuid = Guid.NewGuid().ToString ()
        
        let converters : JsonConverter[] = [|UnionConverter<Result<Object>> (); UnionConverter<Command> ()|]
        
        Zmq.RequestResponse.responder endpoint context
        |> startService (serialize converters) (deserialize converters) (killGuid)
        |> Async.Start

    [<EntryPoint>]
    let main argv =
        use context = ZmqContext.Create()
        initializeService context "tcp://*:5556"
        Console.ReadLine() |> ignore
        0