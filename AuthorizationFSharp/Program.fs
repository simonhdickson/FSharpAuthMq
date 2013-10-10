open System
open System.Collections.Generic
open System.Linq
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

module Server =
    type Worker =
        { Address:byte[]; mutable Expires:DateTime }

    let resetWorker (queue:Queue<Worker>) timeout identity =
        { Address=identity; Expires=DateTime.Now.AddSeconds(timeout) }

    let routeWorkerMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Worker>) _ =
        let message = routerFrom.ReceiveMessage()
        let workerAddress = message.Unwrap().Buffer;

        if not(queue |> Seq.exists (fun i -> i.Address.SequenceEqual(workerAddress))) then
            queue.Enqueue
                { Address=workerAddress; Expires=DateTime.Now.AddSeconds(20.0) }

        match Encoding.UTF8.GetString(message.[0].Buffer) with
        | "Ready" ->
            printfn "Worker has connected"
        | "Heartbeat" ->
            printfn "Heartbeat from worker"
            let worker =
                queue |> Seq.find (fun i -> i.Address.SequenceEqual(workerAddress))
            worker.Expires <- DateTime.Now.AddSeconds(20.0)
        | _ ->
            routerTo.SendMessage(message) |> ignore

        let idleWorkers =
            queue |> Seq.where (fun i -> i.Expires < DateTime.Now)

        for worker in idleWorkers do
            printfn "Idle workers: %i" (Seq.length idleWorkers)
            let message = new ZmqMessage([|Encoding.UTF8.GetBytes("Heartbeat")|])
            message.Wrap(new Frame(worker.Address))
            routerFrom.SendMessage message |> ignore

    let routeClientMessages (routerFrom:ZmqSocket) (routerTo:ZmqSocket) (queue:Queue<Worker>) _ =
        let message = routerFrom.ReceiveMessage()
        let identity = queue.Dequeue()
        message.Wrap(new Frame(identity.Address))
        routerTo.SendMessage(message) |> ignore

    let startRouter heartbeat (frontEnd:ZmqSocket, backEnd:ZmqSocket) =
        async {
            let queue = new Queue<Worker>()
            frontEnd.ReceiveReady.Add (routeClientMessages frontEnd backEnd queue)
            backEnd.ReceiveReady.Add (routeWorkerMessages backEnd frontEnd queue)
            let poller = new Poller([| frontEnd; backEnd |])
            while true do
                poller.Poll(TimeSpan.FromSeconds heartbeat) |> ignore
        }

[<EntryPoint>]
let main argv =
    use context = ZmqContext.Create()
    
    context
    |> Zmq.router "tcp://*:5556" "tcp://*:5557"
    |> Server.startRouter 5.0
    |> Async.Start

    Console.ReadLine() |> ignore
    0
