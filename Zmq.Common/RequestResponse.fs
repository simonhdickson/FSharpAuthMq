namespace Zmq

open ZeroMQ
open System.Text

module RequestResponse =
    type Result<'a> =
        | Success of 'a
        | Timeout
        | Failure

    let requester address (zmqContext:ZmqContext) =
        let requester = zmqContext.CreateSocket SocketType.REQ
        requester.Connect address
        requester

    let responder address (zmqContext:ZmqContext) =
        let responder = zmqContext.CreateSocket SocketType.REP
        responder.Bind address
        responder

    let sendRequest serializer deserializer (requestSocket:ZmqSocket) request timeout =
        let serializedRequest = serializer request
        requestSocket.Send(serializedRequest, Encoding.UTF8) |> ignore
        let reply = requestSocket.Receive(Encoding.UTF8, timeout)
        if reply <> null then
            Success (deserializer reply)
        else
            Timeout