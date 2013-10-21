namespace Container
open System

module Pipeline =
    [<NoComparison>]
    type PipelineState<'t> = { request:'t; environment:Map<string, Object> }

    type PipelineError = { errorMessage:string }

    [<NoComparison>]
    type PipelineContinuation<'t> =
        | Continue  of PipelineState<'t> // the pipeline handler *may have* contributed some effort and wants processing to continue down the pipeline.
        | Handled   of PipelineState<'t> // the pipeline handler has handled the message, abort further processing.
        | Abort     of PipelineError // all pipeline handlers will check this, and if set, immediately pass through.

    let start state = async { return Continue state }

    let private bind nextProcessor asyncPipelineContinuation =
        async {
            let! pipelineContinuation = asyncPipelineContinuation
            match pipelineContinuation with
            | Continue  state   -> return! nextProcessor state
            | Handled   state   -> return Handled state
            | Abort     state   -> return Abort state
        }
    
    let (>>=) input fn = bind fn input