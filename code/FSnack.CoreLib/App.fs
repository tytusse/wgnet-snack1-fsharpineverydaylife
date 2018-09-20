module FSnack.CoreLib.App

open Castle.MicroKernel.Registration
open Castle.Windsor
open System
open Castle.Facilities.Logging
open Castle.Facilities.TypedFactory

type HandlingResult = bool
type Handler<'Event> = 'Event -> Async<HandlingResult>

type IHandlerSelector<'Event> =
    abstract MaybeHandler: evt:'Event -> Handler<'Event> option

type IStartable = 
    abstract member Startup: Async<unit>
    abstract member OrdinalNumber: int

type IDispatcher<'Event> =
    abstract Post: 'Event -> unit

type private Dispatcher<'Event>
    (hsFac:ComponentModel.IFactory<IHandlerSelector<'Event>>,
     logger:ILogger,
     token:RefCancellationToken) =
    
    do logger.Info "starting"

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
        new MailboxProcessor<_>(handle, token.Token)
    
    do 
        // crashes service if threading configured properly
        agent.Error.Add(fun e -> raise<| exn("Queue error", e))
        agent.Start()

    interface IDispatcher<'Event> with
        member __.Post(evt:'Event) = agent.Post(evt)

let runWith (extraConfig:IWindsorContainer->IWindsorContainer) =
    let cancellation = new CancellationTokenSource()
    let wc = 
        (new WindsorContainer())
        |> Windsor.addFacility<Windsor.FsOptionFacility>
        |> Windsor.addFacility<TypedFactoryFacility>
        |> Windsor.addFacilityWith(fun (x:LoggingFacility) -> 
            x.LogUsing<Castle.Core.Logging.ConsoleFactory>())
        |> Windsor.registerSome [
            Component.For<RefCancellationToken>().Instance({Token = cancellation.Token})
            Component
                .For(typedefof<ComponentModel.IFactory<_>>)
                .AsFactory()
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
        |> extraConfig
    
    let startables = wc.ResolveAll<IStartable>()
    match
        startables 
        |> Array.sortBy(fun s -> s.OrdinalNumber)
        // we can implement more elaborate strategy for startup, i.e. group by
        // ordinal no and start each group separately
        // here for simplicity just run it on this thread
        |> Array.map(fun s ->  Result.maybeExn Async.RunSynchronously s.Startup)
        |> Result.errors
        |> List.ofSeq
        with
    | [] -> () 
    | errs -> raise <| AggregateException errs

    // better in PRD
    Console.ReadLine() |> ignore
    printfn " Cancelling..."
    cancellation.Cancel()

let run () = runWith id
    


