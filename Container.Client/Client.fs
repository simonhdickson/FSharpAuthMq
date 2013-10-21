namespace Container
open System
open System.Text
open ZeroMQ
open Client.Authentication
open Zmq.RequestResponse
  
module Client =
    [<NoComparison>]
    type Operation<'msg, 'res> =
      | Send of 'msg * AsyncReplyChannel<'res> * TimeSpan
      | Kill
      
    type Client<'a,'b>(mailbox:MailboxProcessor<Operation<'a, 'b>>) =
        member x.send message =
            mailbox.PostAndReply (fun replyChannel -> Send (message, replyChannel, TimeSpan.FromSeconds 2.0))
        member x.kill () =
            mailbox.Post Kill

    let createRequester serializer deserializer context endpoint =
        let ctx = match context with
                  | Some ctx -> ctx
                  | None -> ZmqContext.Create ()

        new Client<_,_> (MailboxProcessor.Start
            (fun inbox ->
                let socket = ctx.CreateSocket(SocketType.REQ)
                socket.Connect(endpoint)
                socket.Linger <- System.TimeSpan.FromSeconds 0.1
                let rec loop (socket:ZmqSocket) =
                    async  { 
                        let! recieved = inbox.Receive()
                        match recieved with
                        | Send (msg, replyChannel, timeout) ->
                            socket.Send(msg |> serializer, Encoding.UTF8, timeout) |> ignore
                            match socket.Receive(Encoding.UTF8) with
                            | null -> replyChannel.Reply Failure
                            | response -> response |> deserializer |>  replyChannel.Reply
                            do! loop socket
                        | Kill -> return ()
                    }
                loop socket
            ))