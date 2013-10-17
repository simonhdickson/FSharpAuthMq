namespace Zmq

open ZeroMQ

module Router =
    let router frontend backend  (zmqContext:ZmqContext) =
        let router = zmqContext.CreateSocket SocketType.ROUTER
        let dealer = zmqContext.CreateSocket SocketType.ROUTER
        router.Linger <- System.TimeSpan.FromSeconds 1.0
        dealer.Linger <- System.TimeSpan.FromSeconds 1.0
        router.Bind frontend
        dealer.Bind backend
        (router, dealer)

