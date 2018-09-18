module FSnack.CoreLibTests.Windsor

open Xunit
open Castle.Windsor
open System
open Castle.MicroKernel.Registration
open Castle.Facilities.TypedFactory

type MyDisposable() =
    let mutable disposed = false
    member __.Disposed = disposed
    interface IDisposable with member __.Dispose() = disposed <- true

type IFactory<'T> =
    abstract ResolveAll: unit -> 'T[]
    abstract ReleaseAll: 'T[] -> unit
    abstract Resolve: unit -> 'T
    abstract Release: 'T -> unit

[<Fact>]
let ``WindsorContainer.Release->will not release array of components``() =
    let wc = 
        (new WindsorContainer())
            .Register(
                Component.For<MyDisposable>().LifestyleTransient().Named("a"),
                Component.For<MyDisposable>().LifestyleTransient().Named("b"))
    
    let comps = wc.ResolveAll<MyDisposable>()
    Assert.Equal(2, comps.Length)

    wc.Release(comps)

    Assert.All(comps, fun c -> Assert.False c.Disposed)

[<Fact>]
let ``Typed factory does not support releasing multiple components``() =
    let wc = 
        (new WindsorContainer())
            .AddFacility<TypedFactoryFacility>()
            .Register(
                Component.For(typedefof<IFactory<_>>).LifestyleSingleton().AsFactory(),
                Component.For<MyDisposable>().LifestyleTransient().Named("a"),
                Component.For<MyDisposable>().LifestyleTransient().Named("b"))
    
    
    let f = wc.Resolve<IFactory<MyDisposable>>()
    let comps = f.ResolveAll()
    Assert.Equal(2, comps.Length)

    f.ReleaseAll comps

    Assert.All(comps, fun c -> Assert.False c.Disposed)

[<Fact>]
let ``Typed factory does support releasing``() =
    let wc = 
        (new WindsorContainer())
            .AddFacility<TypedFactoryFacility>()
            .Register(
                Component.For(typedefof<IFactory<_>>).LifestyleSingleton().AsFactory(),
                Component.For<MyDisposable>().LifestyleTransient())
    
    
    let f = wc.Resolve<IFactory<MyDisposable>>()
    let comp = f.Resolve()
    
    f.Release comp

    Assert.True comp.Disposed


