namespace Service

open System
open System.Text
open ServiceHost.Pipeline

/// Back-end for the Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }
    type Revoke = { username:string }

    type Command =
        | Authenticate of Authenticate
        | Revoke of Revoke

    let authenticate (pipelineState:PipelineState<Command>) =
        async {
            match pipelineState.request with
            | Authenticate command ->
                let isAuthenticated = command.username = "jbloggs" && command.password = "letmein"
                return PipelineContinuation.Handled ({ pipelineState with environment=(pipelineState.environment.Add("result", isAuthenticated)) })
            | _ ->
                return PipelineContinuation.Continue pipelineState
        }

    let revoke (pipelineState:PipelineState<Command>) =
        async {
            match pipelineState.request with
            | Revoke command ->
                let isRevoked = command.username = "jbloggs"
                return PipelineContinuation.Handled ({ pipelineState with environment=(pipelineState.environment.Add("result", isRevoked)) })
            | _ ->
                return PipelineContinuation.Continue pipelineState
        }