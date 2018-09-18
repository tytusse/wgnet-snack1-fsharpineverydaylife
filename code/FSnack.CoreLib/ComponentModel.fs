module FSnack.CoreLib.ComponentModel

open System

type AutoRelease<'Component>(cmp:'Component, releaser) =
    member val Value = cmp
    interface IDisposable with member __.Dispose() = releaser cmp

type IFactory<'Component> =
    abstract Create: unit -> 'Component 
    abstract CreateAll: unit -> seq<'Component>
    abstract Release: 'Component -> unit

type IProvider<'T> =
    abstract Instance: 'T

let delegatedProvider<'T> name f = { 
    new obj() with override __.ToString() = name
    interface IProvider<'T> with member __.Instance = f()
}
    
