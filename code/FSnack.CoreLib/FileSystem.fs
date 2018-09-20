module FSnack.CoreLib.FileSystem

open System.IO

type IWatcher =
    inherit System.IDisposable
    abstract Credated: IEvent<string>

type Interface =
    abstract ListFiles: path:string -> seq<string>
    abstract OpenWrite: path:string -> Stream
    abstract OpenRead: path:string -> Stream
    abstract WatchDir: path:string -> Async<IWatcher>

type private DiskWatcher(path:string) = 
    
    let w = new FileSystemWatcher(path)
    let evt = w.Created |> Event.map(fun x -> x.FullPath)

    do w.EnableRaisingEvents <- true

    interface IWatcher with
        member __.Credated = evt
        member __.Dispose() = w.Dispose()
    
type Disk() =
    interface Interface with
        member __.ListFiles(path) = Directory.EnumerateFiles path
        member __.OpenRead(path) = File.OpenRead path :> Stream
        member __.OpenWrite(path) = File.OpenWrite path :> Stream
        member __.WatchDir(path) = async { return new DiskWatcher(path) :> IWatcher}