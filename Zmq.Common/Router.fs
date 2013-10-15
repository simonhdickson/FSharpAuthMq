namespace Zmq

open ZeroMQ

module Router =
    let router frontend backend  (zmqContext:ZmqContext) =
        let router = zmqContext.CreateSocket SocketType.ROUTER
        let dealer = zmqContext.CreateSocket SocketType.ROUTER
        router.Bind frontend
        dealer.Bind backend
        (router, dealer)

