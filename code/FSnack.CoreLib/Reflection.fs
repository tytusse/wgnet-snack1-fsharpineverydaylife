module FSnack.CoreLib.Reflection

open System

let(|Generic|_|) (typeDef:Type) (x:Type) =
    if typeDef.IsGenericTypeDefinition && x.IsGenericType && typeDef = x.GetGenericTypeDefinition() then
        Some(x.GetGenericArguments())
    else None

let (|Type|) (o:obj) = o.GetType()

