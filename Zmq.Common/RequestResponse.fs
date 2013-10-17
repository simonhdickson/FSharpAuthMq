namespace Zmq

open ZeroMQ
open System.Text

module RequestResponse =
    type Result<'a> =
        | Success of 'a
        | Failure

    let responder address (zmqContext:ZmqContext) =
        let responder = zmqContext.CreateSocket SocketType.REP
        responder.Bind address
        responder.Linger <- System.TimeSpan.FromSeconds 1.0
        responder