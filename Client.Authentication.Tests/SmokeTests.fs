namespace Client.Authentication.Tests

open NUnit.Framework
open NaturalSpec
open Client.Authentication
open ClientHost
open ZeroMQ
open Zmq.RequestResponse
open Host
open Serializer.Json
open Newtonsoft.Json
open Newtonsoft.Json.FSharp



module SmokeTests =
    let endpoint = "inproc://UserService"

    [<Scenario>]
    let ``given a good user pass combo when authenticated then it succeeds`` () =
        let context = ZmqContext.Create ()
        
        let killServiceFunction = ServiceHost.initializeService context endpoint 

        let converters : JsonConverter[] = [|UnionConverter<Command> ()|]

        let mailbox = ClientHost.createRequester (serialize converters) (deserialize [||]) (Some context) endpoint


        let ``we authenticate`` (username, password) =
            ClientHost.send mailbox (Authenticate { username=username; password=password; })

        Given ("jbloggs", "letmein")
        |> When ``we authenticate``
        |> It should equal (Result.Success true)
        |> Verify

        context.Terminate ()

        

    [<Scenario>]
    let ``chickens should not be allowed`` () =
        let ``we authenticate`` (username, password) =
            use context = ZmqContext.Create ()
            authenticate context endpoint username password

        Given ("chicken", "squark!")
        |> When ``we authenticate``
        |> It should equal (Result.Success false)
        |> Verify

    [<Scenario>]
    let ``given a valid user they are revoked`` () =
        let ``we revoke`` username =
            use context = ZmqContext.Create ()
            revokeUser context endpoint username

        Given "jbloggs"
        |> When ``we revoke``
        |> It should equal (Result.Success true)
        |> Verify

    [<Scenario>]
    let ``given an invalid user revoke fails`` () =
        let ``we revoke`` username =
            use context = ZmqContext.Create ()
            revokeUser context endpoint username

        Given "arthur"
        |> When ``we revoke``
        |> It should equal (Result.Success false)
        |> Verify