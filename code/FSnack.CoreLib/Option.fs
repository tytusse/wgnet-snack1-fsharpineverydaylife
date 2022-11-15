module FSnack.CoreLib.Option

open System.Reflection

type ComputationBuilder() =
    member inline _.Return(v) = Some v
    member inline _.ReturnFrom(m) = m
    member inline _.Zero() = None
    member inline _.Bind(m, [<InlineIfLambda>]f) = 
        match m with
        | Some m' -> f(m')
        | _ -> None

let computationBuilder = ComputationBuilder()

let optionTypeOpen = typedefof<_ option> 

let isOptionType = function
| Reflection.Generic optionTypeOpen _ -> true
| _ -> false

let createDynamicWrapper t = 
    let access = BindingFlags.Static ||| BindingFlags.Public
    let dst = typedefof<option<_>>.MakeGenericType([|t|])
    let some = dst.GetMethod("Some", access)
    let none = dst.GetMethod("get_None", access)
    let some (v:obj) = some.Invoke(null, [|v|]) // Emmit version will have cast and dynamic method here
    let fail() = none.Invoke(null, [||]) // ^--- same as above
    some, fail

