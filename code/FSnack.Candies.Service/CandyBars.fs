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
    member __.Handler(Events.NewFileEvent path) = async { 
        use str = new System.IO.StreamReader(fs.OpenRead path)
        let! txt = str.ReadToEndAsync() |> Async.AwaitTask
        let xml = Xml.Parse txt
        let now = date.Instance
        let good, bad = 
            xml.Candies 
            |> Array.map(mapCandy now)
            |> Result.partition
        
        bad
        |> Seq.map(fun (e,c) -> sprintf "    %A = %s" c e)
        |> String.concat "\n"
        |> logger.Errorf "Following candies are not good:\n %s"

        good
        |> Seq.map(fun c -> sprintf "    %s, Remark=%A" c.Name c.Remark )
        |> String.concat "\n"
        |> logger.Errorf "Following candies are not good:\n %s"

        return List.isEmpty bad 
    }

// explicitly use module names
type Selector(fac:ComponentModel.IFactory<Import>) =
    interface App.IHandlerSelector<Events.NewFileEvent> with
        member __.MaybeHandler (Events.NewFileEvent newFile) = 
            if newFile.EndsWith(".candybars.xml", System.StringComparison.OrdinalIgnoreCase)
            then Some(fun x ->
                use impl = fac.CreateAutorelease()
                impl.Value.Handler x)
            else None

type Installer() =
    interface IWindsorInstaller with
        member __.Install(c, _) = 
            c.Register(
                Component
                    .For<App.IHandlerSelector<Events.NewFileEvent>>()
                    .ImplementedBy<Selector>())
            |> ignore

