open System
open System.Collections.Generic
open System.Threading
open System.Text
open ZeroMQ

module Zmq =
    let worker address (zmqContext:ZmqContext) =
        let dealer = zmqContext.CreateSocket SocketType.DEALER
        dealer.Connect address
        dealer

module Worker =
    type Response =
    | Heartbeat of float
    | Ok
    | Timeout

    let processRequest requestProcessor (workerSocket:ZmqSocket) (timeoutSeconds:float) =
        let message = workerSocket.ReceiveMessage(TimeSpan.FromSeconds(timeoutSeconds))
        match message.IsComplete with
        | false -> Timeout
        | true->
            let identity = message.Unwrap()
            match Encoding.UTF8.GetString(message.Last.Buffer) with
            | "Heartbeat" ->
                Heartbeat 5.0
            | request ->
                let (response:string) =
                    requestProcessor request |> Async.RunSynchronously
                let target = message.Unwrap()
                let responseMessage = new ZmqMessage([|Encoding.UTF8.GetBytes(response)|]);
                responseMessage.Wrap(target)
                workerSocket.SendMessage(responseMessage) |> ignore
                Ok

    let sendHeartbeat heartbeatTime (workerSocket:ZmqSocket) =
        if DateTime.Now > heartbeatTime then
            workerSocket.Send("Heartbeat", Encoding.UTF8) |> ignore

    let rec loop requestProcessor (heartbeatSeconds:float) (workerSocket:ZmqSocket) =
        async {
            let timeout = heartbeatSeconds
            let nextHeartbeat = DateTime.Now.Add(TimeSpan.FromSeconds(heartbeatSeconds))
            let requestResult =
                processRequest requestProcessor workerSocket timeout
            match requestResult with
            | Ok ->
                do! loop requestProcessor heartbeatSeconds workerSocket
            | Timeout ->
                workerSocket |> sendHeartbeat nextHeartbeat
                do! loop requestProcessor (heartbeatSeconds*2.0) workerSocket
            | Heartbeat (seconds)->
                do! loop requestProcessor seconds workerSocket
        }

    let start requestProcessor heartbeatSeconds (workerSocket:ZmqSocket) =
        async {
            workerSocket.Send("Ready", Encoding.UTF8) |> ignore
            do! loop requestProcessor heartbeatSeconds workerSocket
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


let isAuthorizedSlow (message:string) =
    async {
        printfn "Recieved message: %s" message
        Thread.Sleep 60000
        let x = message.Split ','
        if x.[0] = "Bob" && x.[1] = "1" then
            return true.ToString()
        else
            return false.ToString()
    }

[<EntryPoint>]
let main argv = 
    use context = ZmqContext.Create()
    let heartbeat = 5.0
    
    for i in 1..20 do
        context
        |> Zmq.worker "tcp://localhost:5557"
        |> Worker.start isAuthorized heartbeat
        |> Async.Start

    Console.ReadLine() |> ignore
    0
