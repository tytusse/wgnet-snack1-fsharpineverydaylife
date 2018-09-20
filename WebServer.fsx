open System
open System.Net
open System.IO
// based on: http://www.fssnip.net/1X/title/Simple-HTTP-server-with-Async-workflow
// needs to run as admin or run this first in console (with admin privleges):
// - netsh http add urlacl url=http://*:8070/ user=GIERCOL\Tomek

type HttpListener with
    static member Run (url:string,handler: (HttpListenerRequest -> HttpListenerResponse -> Async<unit>)) = 
        let listener = new HttpListener()
        listener.Prefixes.Add url
        listener.Start()
        let asynctask = Async.FromBeginEnd(listener.BeginGetContext,listener.EndGetContext)
        async {
            while true do 
                let! context = asynctask
                Async.Start (handler context.Request context.Response)
        } |> Async.Start 
        listener

HttpListener.Run("http://*:8070/",(fun req resp -> 
        let root = "docs"
        async {
            printfn "handling %O" req.Url

            let urlpath = 
                let path =
                    match req.Url.PathAndQuery.IndexOf("?") with
                    | -1 -> req.Url.PathAndQuery
                    | idx -> req.Url.PathAndQuery.Substring(0, idx)
                
                match path with
                | ""|"/" -> "index.html"
                | x when x.StartsWith "/" -> x.Substring 1
                | x -> x

            printfn "url path: %s" urlpath

            let fullpath = Path.Combine(root, urlpath)
            printfn "full path: %s" fullpath
            if File.Exists fullpath
            then
                printfn "file found"
                use fstr = File.OpenRead(fullpath)
                do! fstr.CopyToAsync(resp.OutputStream) |> Async.AwaitTask
            else 
                printfn "not found"
                resp.StatusCode <- 404
                resp.StatusDescription <- "not found"   
            
            resp.OutputStream.Close()         
        }
    )) |> ignore
System.Diagnostics.Process.Start(@"http://localhost:8070/")