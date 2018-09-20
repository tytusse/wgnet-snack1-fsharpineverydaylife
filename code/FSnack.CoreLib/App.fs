module FSnack.CoreLib.App

open Castle.MicroKernel.Registration
open Castle.Windsor
open System
open Castle.Facilities.Logging

type HandlingResult = bool
type Handler<'Event> = 'Event -> Async<HandlingResult>

type IHandlerSelector<'Event> =
    abstract MaybeHandler: evt:'Event -> Handler<'Event> option

type IStartable = 
    abstract member Start: unit -> unit
    abstract member OrdinalNumber: int

type IDispatcher<'Event> =
    abstract Post: 'Event -> unit

//type IConfig =

type private Dispatcher<'Event>
    (hsFac:ComponentModel.IFactory<IHandlerSelector<'Event>>,
     logger:ILogger,
     token:CancellationToken) =

    let handleEvt evt = async {
        try
            use selectors = hsFac.ResolveAllAutorelease()
            let inline maybeHandler (sel:IHandlerSelector<_>) = sel.MaybeHandler evt
            match selectors.Value |> Seq.tryPick maybeHandler with
            | None -> logger.Warnf "Could not find handler for event %O" evt
            | Some handler ->
                let! result = handler evt
                if result
                then logger.Infof "Successfully handled event %O" evt
                else logger.Errorf "Could not handle event %O" evt
        with x -> logger.Error(sprintf "Failed handling event %O" evt, x)
    }
    
    let agent = 
        let rec handle (mb:MailboxProcessor<_>) = async {
            let! evt = mb.Receive()
            do! handleEvt evt
            return! handle mb
        }
        new MailboxProcessor<_>(handle, token)
    
    do 
        // crashes service if threading configured properly
        agent.Error.Add(fun e -> raise<| exn("Queue error", e))
        agent.Start()

    interface IDispatcher<'Event> with
        member __.Post(evt:'Event) = agent.Post(evt)

let run () =
    let wc = 
        (new WindsorContainer())
        |> Windsor.addFacility<Windsor.FsOptionFacility>
        |> Windsor.addFacilityWith(fun (x:LoggingFacility) -> 
            // in PRD other logging obviousely
            x.LogUsing<Castle.Core.Logging.ConsoleFactory>())
        |> Windsor.registerSome [
            Component
                .For(typedefof<IDispatcher<_>>)
                .ImplementedBy(typedefof<Dispatcher<_>>)
            Component
                .For<ComponentModel.IProvider<System.DateTime>>()
                .Instance(
                    ComponentModel.delegatedProvider "DateTime.UtcNow" System.DateTime.get_UtcNow)
                .IsFallback()
                
        ]
        |> Windsor.installSome [
            Installer.FromAssembly.This()
            Installer.FromAssembly.Instance(System.Reflection.Assembly.GetEntryAssembly())
        ]
    
    let startables = wc.ResolveAll<IStartable>()
    match
        startables 
        |> Array.sortBy(fun s -> s.OrdinalNumber)
        |> Array.map(fun s -> Result.maybeExn s.Start ())
        |> Result.errors
        |> List.ofSeq
        with
    | [] -> () //OK
    | errs -> raise <| AggregateException errs
    


