[<AutoOpen>]
module FSnack.CoreLib.CoreTypeExtensions

open Castle.Core.Logging
open System

type ComponentModel.IFactory<'Component> with
    member fac.CreateAutorelease<'Component>() =
        new ComponentModel.AutoRelease<'Component>
            (fac.Create(), fac.Release)

    member fac.ResolveAllAutorelease<'Service>() =
        let services = fac.CreateAll()
        let release services = 
            services
            |> Seq.map(Result.maybeExn fac.Release)
            |> Result.errors
            |> List.ofSeq

        new ComponentModel.AutoRelease<_>
            (services, 
            release
            >>
            function
            | [] -> ()
            | errs -> raise <| AggregateException(errs))

//type Castle.MicroKernel.IKernel with
//    member kernel.ResolveAutorelease<'Service>() =
//        let service = kernel.Resolve<'Service>()
//        new ComponentModel.AutoRelease<'Service>
//            (service, fun c -> c :> obj |> kernel.ReleaseComponent)

//    member kernel.ResolveAllAutorelease<'Service>() =
//        let services = kernel.ResolveAll<'Service>()
//        let release services = 
//            services
//            |> Array.map(fun s -> Result.maybeExn kernel.ReleaseComponent (s :> obj) )
//            |> Result.errors
//            |> List.ofSeq

//        new ComponentModel.AutoRelease<'Service[]>
//            (services, 
//            release
//            >>
//            function
//            | [] -> ()
//            | errs -> raise <| AggregateException(errs))



