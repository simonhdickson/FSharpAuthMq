namespace Container.Pipeline.Tests
open NUnit.Framework
open NaturalSpec
open Container
open Container.Pipeline

module Tests =
    let cleggProcessor x = async {return Continue x}
    let blairProcessor x = async {return Handled x}
    let cameronProcessor x = async {return Abort x}
    let thatcherProcessor result x =
        async {
            return Handled ({x with environment=(x.environment |> Map.add "result" result )})
        }
    
    [<Scenario()>]
    let ``processing without changing state shouldn't change state`` () =
        let state =  { request=0; environment=Map.empty }

        let pipelineResult () =
             state
             |>  Pipeline.start 
             ||> cleggProcessor 
             ||> blairProcessor
             |> Async.RunSynchronously

        Given ()
        |> When pipelineResult
        |> It should equal (Handled state)
        |> Verify

    [<Scenario()>]
    let ``if a processor returns a result the state should contain it`` () =
        let state =  { request=0; environment=Map.empty }

        let pipelineResult () =
             state
             |>  Pipeline.start 
             ||> thatcherProcessor 1
             |> Async.RunSynchronously

        let extractResult state =
            match state with
            | Handled s -> s.environment.Item "result"
            | Handled s -> 

        Given ()
        |> When pipelineResult
        |> It should equal (Handled state)
        |> Verify