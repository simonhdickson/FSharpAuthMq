open System
open System.Collections.Generic
open System.Threading
open System.Text
open ZeroMQ

module Zmq =
    let router frontend backend  (zmqContext:ZmqContext) =
        let router = zmqContext.CreateSocket SocketType.ROUTER
        let dealer = zmqContext.CreateSocket SocketType.ROUTER
        router.Bind frontend
        dealer.Bind backend
        (router, dealer)

    let worker address (zmqContext:ZmqContext) =
        let dealer = zmqContext.CreateSocket SocketType.REQ
        dealer.Connect address
        dealer

module Server =
    let routeWorkerMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
        let message = routerFrom.ReceiveMessage()
        queue.Enqueue(message.Unwrap())
        if Encoding.UTF8.GetString(message.[0].Buffer) <> "Ready" then
            routerTo.SendMessage(message) |> ignore

    let routeClientMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Frame>) _ =
        let message = routerFrom.ReceiveMessage()
        message.Wrap(queue.Dequeue())
        routerTo.SendMessage(message) |> ignore

    let startRouter (frontEnd:ZmqSocket, backEnd:ZmqSocket) =
        async {
            let queue = new Queue<Frame>()
            frontEnd.ReceiveReady.Add (routeClientMessages frontEnd backEnd queue)
            backEnd.ReceiveReady.Add (routeWorkerMessages backEnd frontEnd queue)
            let poller = new Poller([| frontEnd; backEnd |])
            while true do
                poller.Poll() |> ignore
        }

    let createWorker requestProcessor (workerSocket:ZmqSocket) =
        async {
            workerSocket.Send("Ready", Encoding.UTF8) |> ignore
            while true do
                let message = workerSocket.ReceiveMessage()
                let respondTo = message.Unwrap()
                let request = Encoding.UTF8.GetString(message.[0].Buffer)
                let! (response:string) = requestProcessor request
                let responseMessage = new ZmqMessage()
                responseMessage.Push(new Frame(Encoding.UTF8.GetBytes(response)))
                responseMessage.Wrap(respondTo)
                workerSocket.SendMessage(responseMessage) |> ignore
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
   
    for i in 1 .. 10 do
        context
        |> Zmq.worker "inproc://backend"
        |> Server.createWorker isAuthorized
        |> Async.Start

    Console.ReadLine() |> ignore
    0
