namespace Server

open System
open System.Text
open ZeroMQ
open Newtonsoft.Json

module Json =
    let convertFrom (converters:JsonConverter[]) v = 
        JsonConvert.SerializeObject(v,Formatting.Indented,converters)

    let convertTo (converters:JsonConverter[]) v = 
        JsonConvert.DeserializeObject<'t>(v,converters)

module Zmq =
    let responder addresss (zmqContext:ZmqContext) =
        let responder = zmqContext.CreateSocket SocketType.REP
        responder.Bind addresss
        responder

module Server =
    let start requestProcessor serialize deserialize (recieveSocket:ZmqSocket) =
        async {
            while true do
                let request =
                    recieveSocket.Receive Encoding.UTF8 |> deserialize
                let! response = requestProcessor request
                recieveSocket.Send(response |> serialize, Encoding.UTF8) |> ignore
        }