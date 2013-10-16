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
        |> It should equal (Result.Success true)
        |> Verify

    [<Scenario>]
    let ``chickens should not be allowed`` () =
        let ``we authenticate`` (username, password) =
            use context = ZmqContext.Create ()
            authenticate context username password

        Given ("chicken", "squark!")
        |> When ``we authenticate``
        |> It should equal (Result.Success false)
        |> Verify

    [<Scenario>]
    let ``given a valid user they are revoked`` () =
        let ``we revoke`` username =
            use context = ZmqContext.Create ()
            revokeUser context username

        Given "jbloggs"
        |> When ``we revoke``
        |> It should equal (Result.Success true)
        |> Verify

    [<Scenario>]
    let ``given an invalid user revoke fails`` () =
        let ``we revoke`` username =
            use context = ZmqContext.Create ()
            revokeUser context username

        Given "arthur"
        |> When ``we revoke``
        |> It should equal (Result.Success false)
        |> Verify