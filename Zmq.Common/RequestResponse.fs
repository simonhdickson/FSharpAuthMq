namespace Zmq

open ZeroMQ
open System.Text

module RequestResponse =
    let requester address (zmqContext:ZmqContext) =
        let requester = zmqContext.CreateSocket SocketType.REQ
        requester.Connect address
        requester

    let responder address (zmqContext:ZmqContext) =
        let responder = zmqContext.CreateSocket SocketType.REP
        responder.Bind address
        responder

    let sendRequest serializer deserializer (requestSocket:ZmqSocket) request =
        let serializedRequest = serializer request
        requestSocket.Send(serializedRequest, Encoding.UTF8) |> ignore
        let reply = requestSocket.Receive Encoding.UTF8
        let deserializedReply = deserializer reply
        deserializedReply