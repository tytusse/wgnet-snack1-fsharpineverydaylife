module FSnack.CoreLib.Result

type ComputationBuilder() =
    member inline _.Return(v) = Ok v
    member inline _.ReturnFrom(m) = m
    member inline _.Bind(m, [<InlineIfLambda>]f) =
        match m with
        | Ok s -> f s
        | Error f -> Error f
    member inline _.Zero() = Ok()

let computationBuilder = ComputationBuilder()

let errors ps =
    let p p=
        match p with
        | Ok _ -> None
        | Error x -> Some x
    ps |> Seq.choose p

let partition results =
    ([], [])
    |> Seq.foldBack(fun r (ok, bad) ->
        match r with
        | Ok r -> (r::ok, bad)
        | Error e -> (ok, e::bad)
        )
        results

let asOption = function
| Ok s -> Some s
| Error _ -> None

let asChoice = function
| Ok s -> Choice1Of2 s
| Error f -> Choice2Of2 f

let Errorf format = Printf.kprintf (Error) format

let maybeExn f x =
    try f x |> Ok
    with x -> Error x

