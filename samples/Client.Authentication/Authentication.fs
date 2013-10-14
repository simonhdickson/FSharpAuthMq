namespace Client

open System
open System.Text
open ZeroMQ
open Newtonsoft.Json
open Serializer.Json

/// Client library for Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    let authenticate (context:ZmqContext) username password : bool =
        let authenticationRequest =
            context
            |> Zmq.RequestResponse.requester "tcp://localhost:5556"
            |> Zmq.RequestResponse.sendRequest (serialize [||]) (deserialize [||])

        let isAuthorized = authenticationRequest { username=username; password=password; }
        isAuthorized