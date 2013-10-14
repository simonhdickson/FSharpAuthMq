namespace Client.Authentication.Tests

open NUnit.Framework
open NaturalSpec
open Client.Authentication
open ZeroMQ

//[<TestFixture>]
//type ``Given a good user pass combo`` ()=
//    [<Test>] member test.``when authenticated then it succeeds`` ()=
//                authenticate (ZmqContext.Create ()) "jbloggs" "letmein" |> should equal True

module SmokeTests =
    let ``given a good user pass combo when authenticated then it succeeds`` () =
        let ``we authenticate`` (username, password) =
            authenticate (ZmqContext.Create ()) username password

        Given ("jbloggs", "letmein")
        |> When ``we authenticate``
        |> It should equal true
        |> Verify