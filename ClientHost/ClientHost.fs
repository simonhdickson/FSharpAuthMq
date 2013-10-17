open System
open System.Text
open ZeroMQ
open Client.Authentication
open Zmq.RequestResponse
  
module ClientHost =

    type Operation<'msg, 'res> =
      | Send of 'msg * AsyncReplyChannel<'res> * TimeSpan


    let createRequester serializer (deserializer: string -> _) context endpoint =
        let ctx = match context with
                  | Some ctx -> ctx
                  | None -> ZmqContext.Create ()

        MailboxProcessor.Start
            (
                fun inbox ->
                let socket = ctx.CreateSocket(SocketType.REQ)
                socket.Connect(endpoint)
                let rec loop (socket:ZmqSocket) =
                    async 
                        { 
                            let! recieved = inbox.Receive()
                            match recieved with
                            | Send (msg, replyChannel, timeout) ->
                                socket.Send(msg |> serializer, Encoding.UTF8, timeout) |> ignore
                                socket.Receive(Encoding.UTF8) 
                                |> deserializer
                                |> replyChannel.Reply 

                                do! loop socket
                        }
                loop socket
            )


    let send (mailbox:MailboxProcessor<Operation<'a, 'b>>) message=
        mailbox.PostAndReply (fun replyChannel -> Send (message, replyChannel, TimeSpan.FromSeconds 2.0))

    