open System
open System.Threading
open System.Text
open ZeroMQ

module Zmq =
    let requester addresss (zmqContext:ZmqContext) =
        let requester = zmqContext.CreateSocket(SocketType.REQ)
        requester.Connect addresss
        requester

module Client =
    type Response =
    | Timeout
    | Ok of string

    let sendRequest timeout (requestSocket:ZmqSocket) request =
        async {
            requestSocket.Send(request, Encoding.UTF8) |> ignore
            let response =
                requestSocket.Receive(Encoding.UTF8, TimeSpan.FromSeconds(timeout))
            if (response = null) then
                return Timeout
            else
                return Ok(response)
        }

open Client

let requests =
    [("Bob",1); ("Bob",2); ("Alice",1)]

let rec requestLoop tries context name id =
    async {
        if tries = 0 then
            return Timeout
        else
            use socket = Zmq.requester "tcp://localhost:5556" context
            let authorizationRequest = Client.sendRequest 10.0 socket
            let! isAuthorized =
                authorizationRequest (sprintf "%s,%i" name id)
            match isAuthorized with
            | Ok(result) ->
                return Ok(result)
            | Timeout ->
                return! requestLoop (tries-1) context name id
    }

let rec loop requests context name id =
    async {
        if requests = 0 then
            return ignore |> ignore
        else
            let! response = requestLoop 3 context name id
            match response with
            | Ok(result) ->
                printfn "%s requested %i, allowed: %s" name id result
            | Timeout ->
                printfn "Timeout"
            do! loop (requests - 1) context name id
    }

[<EntryPoint>]
let main argv = 
    use context = ZmqContext.Create()

    for (name,id) in requests do
        async {
            do! loop 10 context name id
        } |> Async.Start

    Console.ReadLine() |> ignore
    0
