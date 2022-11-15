module FSnack.CoreLib.Windsor

open Castle.Core
open Castle.Windsor
open Castle.MicroKernel
open Castle.MicroKernel.Registration

let addFacility<'T when 'T:(new: unit -> 'T) and 'T :> IFacility> (cnt:IWindsorContainer) = cnt.AddFacility<'T>()
let addFacilityWith<'T, 'Q when 'T:(new: unit -> 'T) and 'T :> IFacility> (f:'T->'Q) (cnt:IWindsorContainer) = cnt.AddFacility<'T>(f>>ignore)
let addFacilityFrom (cnt:IWindsorContainer) facility = cnt.AddFacility(facility)
let register (reg:IRegistration) (cnt:IWindsorContainer) = cnt.Register(reg)
let registerSome (regs) (cnt:IWindsorContainer) = cnt.Register(regs |> Array.ofSeq)
let install<'T when 'T:(new:unit->'T) and 'T :> IWindsorInstaller> (cnt:IWindsorContainer) = cnt.Install([|new 'T() :> IWindsorInstaller|])
let installFrom (installer:IWindsorInstaller) (cnt:IWindsorContainer) = cnt.Install(installer)
let installSome installers (cnt:IWindsorContainer) = cnt.Install(installers |> Array.ofList)
let addSubResolverInstance sdr (cnt:IWindsorContainer) = cnt.Kernel.Resolver.AddSubResolver(sdr);cnt
let addSubResolverDependent sdrf (cnt:IWindsorContainer) = cnt.Kernel.Resolver.AddSubResolver(sdrf cnt);cnt
let addSubResolver<'Sdr when 'Sdr: (new: unit ->'Sdr) and 'Sdr :> ISubDependencyResolver> (cnt:IWindsorContainer) = cnt.Kernel.Resolver.AddSubResolver(new 'Sdr());cnt
let resolve<'T> (cnt:IWindsorContainer) = cnt.Resolve<'T>()

/// Enable using FS option type in constructor parameters as an "optional dependency"
type FsOptionFacility() =

    let optionTypeDef = typedefof<_ option>
    let fixParameter(dependency:DependencyModel) =
        
        let notNull = not<<isNull
        if dependency.TargetType = typeof<string> && notNull(dependency.Parameter) && isNull(dependency.Parameter.Value)
        then dependency.Parameter <- ParameterModel(dependency.Parameter.Name, "")

        dependency

    let subResolver (kernel:IKernel) = { new ISubDependencyResolver with
        member _.CanResolve(_, _, _, dependency) = 
            Option.isOptionType dependency.TargetType
        
        member _.Resolve(context, contextHandlerResolver, model, dependency) = 
            let dependency =
                match dependency.TargetType with
                | Reflection.Generic optionTypeDef [|t|] -> 
                    let q = DependencyModel(dependency.DependencyKey, t, false, dependency.HasDefaultValue, dependency.DefaultValue)
                    q.Init(model.Parameters)
                    q.Parameter <- dependency.Parameter
                    fixParameter q
                | t -> failwith $"Type %A{t} should be generic type with 1 param"

            let some, none = Option.createDynamicWrapper dependency.TargetType
            let parms = context, contextHandlerResolver, model, dependency
            if kernel.Resolver.CanResolve(parms) then
                let obj = kernel.Resolver.Resolve(parms)
                match obj with
                | Reflection.Type(Reflection.Generic optionTypeDef [|t|]) when t = dependency.TargetType -> obj
                | obj -> some obj
            else none()
    }

    let contributor = { new Castle.MicroKernel.ModelBuilder.IContributeComponentModelConstruction with
        member _.ProcessModel(_, model: ComponentModel) = 
            model.Dependencies :> _ seq
            |> Seq.filter (fun d -> d.TargetType |> Option.isOptionType) 
            |> Seq.iter (fun d -> d.IsOptional <- true)
        }

    interface IFacility with
        member _.Init(kernel: IKernel, _) = 
            kernel.Resolver.AddSubResolver(subResolver kernel)
            kernel.ComponentModelBuilder.AddContributor(contributor)
        
        member _.Terminate(): unit = ()

    