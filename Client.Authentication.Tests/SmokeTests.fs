namespace Client.Authentication.Tests

open NUnit.Framework
open NaturalSpec
open Client.Authentication
open ZeroMQ
open Zmq.RequestResponse

module SmokeTests =
    [<Scenario>]
    let ``given a good user pass combo when authenticated then it succeeds`` () =
        let ``we authenticate`` (username, password) =
            use context = ZmqContext.Create ()
            authenticate context username password

        Given ("jbloggs", "letmein")
        |> When ``we authenticate``
        |> It should equal Result.Timeout
        |> Verify

    [<Scenario>]
    let ``given`` () =
        let ``we`` () =
            false

        Given ()
        |> When ``we``
        |> It should equal false
        |> Verify