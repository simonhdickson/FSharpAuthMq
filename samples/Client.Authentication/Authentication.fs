namespace Client

open System
open System.Text
open ZeroMQ
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Serializer.Json
open Zmq.RequestResponse

/// Client library for Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    type RevokeUser = { username:string }
    
    type Command =
        | Authenticate of Authenticate
        | Revoke of RevokeUser

//    type Command' = { command:Command }

//    let private execute context endpoint command =
//        let timeout = TimeSpan.FromSeconds(2.0)
//        use socket = Zmq.RequestResponse.requester endpoint context
//        socket.Linger <- timeout // linger timeout is important because during dispose of ZmqContext it will insist on clearing its output queues.
//        let converters : JsonConverter[] = [|UnionConverter<Command> ()|]
//        Zmq.RequestResponse.execute
//            (serialize converters)
//            (deserialize [||])
//            socket
//            command
//            timeout
//
//    let authenticate (context:ZmqContext) endpoint  username password =
//        execute context endpoint (Authenticate { username=username; password=password; })
//
//    let revokeUser (context:ZmqContext) endpoint username =
//        execute context endpoint (Revoke { username=username })