open System
open System.Threading
open System.Text
open ZeroMQ

module Zmq =
    let router frontend backend  (zmqContext:ZmqContext) =
        let router = zmqContext.CreateSocket SocketType.ROUTER
        let dealer = zmqContext.CreateSocket SocketType.DEALER
        router.Bind frontend
        dealer.Bind backend
        (router, dealer)

    let dealer address (zmqContext:ZmqContext) =
        let dealer = zmqContext.CreateSocket SocketType.DEALER
        dealer.Connect address
        dealer

module Server =
    let routeMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) _ =
        let message = routerFrom.ReceiveMessage()
        routerTo.SendMessage(message) |> ignore

    let startRouter (frontEnd:ZmqSocket, backEnd:ZmqSocket) =
        async {
            frontEnd.ReceiveReady.Add (routeMessages frontEnd backEnd)
            backEnd.ReceiveReady.Add (routeMessages backEnd frontEnd)
            let poller = new Poller([|frontEnd; backEnd|])
            while true do
                poller.Poll() |> ignore
        }

    let createWorker requestProcessor (dealerSocket:ZmqSocket) =
        async {
            while true do
                let message = dealerSocket.ReceiveMessage()
                let respondTo = message.Unwrap()
                let request = Encoding.UTF8.GetString(message.[0].Buffer)
                let! (response:string) = requestProcessor request
                let responseMessage = new ZmqMessage()
                responseMessage.Push(new Frame(Encoding.UTF8.GetBytes(response)))
                responseMessage.Wrap(respondTo)
                dealerSocket.SendMessage(responseMessage) |> ignore
        }

let isAuthorized (message:string) =
    async {
        printfn "Recieved message: %s" message
        Thread.Sleep 1000
        let x = message.Split ','
        if x.[0] = "Bob" && x.[1] = "1" then
            return true.ToString()
        else
            return false.ToString()
    }

[<EntryPoint>]
let main argv =
    use context = ZmqContext.Create()
    
    context
    |> Zmq.router "tcp://*:5556" "inproc://backend"
    |> Server.startRouter
    |> Async.Start
   
    for i in 1 .. 5 do
        context
        |> Zmq.dealer "inproc://backend"
        |> Server.createWorker isAuthorized
        |> Async.Start

    Console.ReadLine() |> ignore
    0
