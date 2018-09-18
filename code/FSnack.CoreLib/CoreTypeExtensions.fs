[<AutoOpen>]
module FSnack.CoreLib.CoreTypeExtensions

open Castle.Core.Logging
open System

type ILogger with
    member x.Logf logMethod fmt = Printf.kprintf (logMethod x) fmt
    member x.Errorf fmt = x.Logf (fun l -> l.Error:string->unit) fmt
    member x.Errorf(ex, fmt) =
        let err msg = x.Error(msg, ex)
        Printf.kprintf err fmt
    member x.Infof fmt = x.Logf (fun l -> l.Info:string->unit) fmt
    member x.Debugf fmt = x.Logf (fun l -> l.Debug:string->unit) fmt
    member x.Warnf fmt = x.Logf (fun l -> l.Warn:string->unit) fmt

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



