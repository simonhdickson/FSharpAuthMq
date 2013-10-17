namespace Client.Authentication.Tests
open NUnit.Framework
open NaturalSpec
open Client.Authentication
open Host.Client
open ZeroMQ
open Zmq.RequestResponse
open Host
open Serializer.Json
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

module SmokeTests =
    let endpoint = "inproc://UserService"

    let startUpService () = 
        let context = ZmqContext.Create ()
        ServiceHost.initializeService context endpoint 
        let converters : JsonConverter[] = [|UnionConverter<Result<bool>> (); UnionConverter<Command> ()|]
        let client = Client.createRequester (serialize converters) (deserialize converters) (Some context) endpoint
        client
        
    [<Scenario>]
    let ``given a good user pass combo when authenticated then it succeeds`` () =
        let client = startUpService ()

        let ``we authenticate`` (username, password) =
            client.send (Authenticate { username=username; password=password; })

        Given ("jbloggs", "letmein")
        |> When ``we authenticate``
        |> It should equal (Result.Success true)
        |> Verify

        client.kill ()

    [<Scenario>]
    let ``chickens should not be allowed`` () =
        let client = startUpService ()

        let ``we authenticate`` (username, password) =
            client.send (Authenticate { username=username; password=password; })

        Given ("chicken", "squark!")
        |> When ``we authenticate``
        |> It should equal (Result.Success false)
        |> Verify

        client.kill ()

    [<Scenario>]
    let ``given a valid user they are revoked`` () =
        let client = startUpService ()

        let ``we revoke`` username =
            client.send (Revoke { username=username; })

        Given "jbloggs"
        |> When ``we revoke``
        |> It should equal (Result.Success true)
        |> Verify

        client.kill ()

    [<Scenario>]
    let ``given an invalid user revoke fails`` () =
        let client = startUpService ()

        let ``we revoke`` username =
            client.send (Revoke { username=username; })

        Given "arthur"
        |> When ``we revoke``
        |> It should equal (Result.Success false)
        |> Verify

        client.kill ()