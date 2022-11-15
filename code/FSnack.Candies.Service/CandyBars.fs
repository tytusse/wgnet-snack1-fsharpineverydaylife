module FSnack.Candies.Service.CandyBars
// open main namespace
open FSnack.CoreLib
open Castle.MicroKernel.Registration

type Xml = FSharp.Data.XmlProvider<Schema="Candies.xsd">
type Candy = {Name:string; Remark:string option; Bbd:System.DateTime}
type private Error = string

let private mapCandy (now:System.DateTime) (candy:Xml.Candy) = result {
    if candy.BestBeforeDate < now
    then return! Result.Error (("Candy is too old!":Error), candy)
    else return {Name=candy.Name; Bbd=candy.BestBeforeDate; Remark=candy.Remarks}
}

type Import
    (
        fs:FileSystem.Interface, 
        date:ComponentModel.IProvider<System.DateTime>,
        logger:ILogger) =
    member _.Handler(Events.NewFileEvent path) = async { 
        use str = new System.IO.StreamReader(fs.OpenRead path)
        let! txt = str.ReadToEndAsync() |> Async.AwaitTask
        let xml = Xml.Parse txt
        let now = date.Instance
        let good, bad = 
            xml.Candies 
            |> Array.map(mapCandy now)
            |> Result.partition
        
        bad
        |> Seq.map(fun (e,c) -> $"    %A{c} = %s{e}")
        |> String.concat "\n"
        |> sprintf "Following candies are not good:\n %s"
        |> logger.Error 

        good
        |> Seq.map(fun c -> $"    %s{c.Name}, Remark=%A{c.Remark}" )
        |> String.concat "\n"
        |> sprintf "Bon apetit with these:\n %s"
        |> logger.Info 

        return List.isEmpty bad 
    }

// explicitly use module names
type Selector(fac:ComponentModel.IFactory<Import>) =
    interface App.IHandlerSelector<Events.NewFileEvent> with
        member _.MaybeHandler (Events.NewFileEvent newFile) = 
            if newFile.EndsWith(".candies.xml", System.StringComparison.OrdinalIgnoreCase)
            then Some(fun x ->
                use impl = fac.CreateAutorelease()
                // BUG here: async escapes outside 'use' scope
                impl.Value.Handler x)
            else None

type Installer() =
    interface IWindsorInstaller with
        member _.Install(c, _) = 
            c.Register(
                Component.For<Import>().LifestyleTransient(),
                Component
                    .For<App.IHandlerSelector<Events.NewFileEvent>>()
                    .ImplementedBy<Selector>())
            |> ignore

