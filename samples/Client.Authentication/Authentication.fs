namespace Client

open System
open System.Text
open ZeroMQ
open Newtonsoft.Json
open Serializer.Json
open Zmq.RequestResponse

/// Client library for Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    type RevokeUser = { username:string }

    let private execute context record =
        let timeout = TimeSpan.FromSeconds(2.0)
        use socket = Zmq.RequestResponse.requester "tcp://localhost:5557" context
        socket.Linger <- timeout // linger timeout is important because during dispose of ZmqContext it will insist on clearing its output queues.
        Zmq.RequestResponse.execute
            (serialize[||])
            (deserialize [||])
            socket
            record
            timeout

    let authenticate (context:ZmqContext) username password =
        execute context { username=username; password=password; }

    let revokeUser (context:ZmqContext) username =
        execute context { username=username }