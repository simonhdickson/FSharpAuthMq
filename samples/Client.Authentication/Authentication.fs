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

    let authenticate (context:ZmqContext) username password : Result<'a> =
        let timeout = TimeSpan.FromSeconds(2.0)
        use socket = Zmq.RequestResponse.requester "tcp://localhost:5556" context
        socket.Linger <- timeout // linger timeout is important because during dispose of ZmqContext it will insist on clearing its output queues.

        let authenticationRequest = Zmq.RequestResponse.sendRequest (serialize [||]) (deserialize [||]) socket

        let isAuthorized = authenticationRequest { username=username; password=password; } timeout
        isAuthorized