namespace Client.Authentication.Tests
open NUnit.Framework
open NaturalSpec
open Client.Authentication
open Container
open Container.Client
open ZeroMQ
open Zmq.RequestResponse
open Serializer.Json
open Newtonsoft.Json
open Newtonsoft.Json.FSharp

module SmokeTests =
    let endpoint = "inproc://UserService"

    let startUpService () = 
        let context = ZmqContext.Create ()
        Service.initializeService context endpoint 
        let converters : JsonConverter[] = [|UnionConverter<Result<bool>> (); UnionConverter<Command> ()|]
        let client = Client.createRequester (serialize converters) (deserialize converters) (Some context) endpoint
        client
        
    [<Example("jbloggs", "letmein", true)>]
    [<Example("james", "scienceyo", false)>]
    let ``authenciate command tests`` username password valid =
        let client = startUpService ()

        let ``we authenticate`` () =
            client.send (Authenticate { username=username; password=password; })

        Given ()
        |> When ``we authenticate``
        |> It should equal (Result.Success valid)
        |> Verify

        client.kill ()
        
    [<Example("jbloggs", true)>]
    [<Example("james", false)>]
    let ``given a valid user they are revoked`` username revoked =
        let client = startUpService ()

        let ``we revoke`` () =
            client.send (Revoke { username=username; })

        Given ()
        |> When ``we revoke``
        |> It should equal (Result.Success revoked)
        |> Verify

        client.kill ()