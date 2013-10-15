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

    let execute (serializer:'a->string) deserializer (requestSocket:ZmqSocket) request timeout =
        let serializedRequest = serializer request
        let message = new ZmqMessage ([|Encoding.UTF8.GetBytes serializedRequest|])
        message.Wrap (new Frame (Encoding.UTF8.GetBytes (request.GetType().Name)))
        requestSocket.SendMessage(message) |> ignore
        let reply = requestSocket.Receive(Encoding.UTF8, timeout)
        if reply <> null then
            Success (deserializer reply)
        else
            Timeout