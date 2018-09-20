module FSnack.CoreLib.FileSystem

open System.IO
open Castle.MicroKernel.Registration

// alias poprawia czytelność kodu
type Path = string

type IWatcher =
    inherit System.IDisposable
    abstract Credated: IEvent<Path>

type Interface =
    abstract ListFiles: path:Path -> Async<list<Path>>
    abstract OpenWrite: path:Path -> Stream
    abstract OpenRead: path:Path -> Stream
    abstract DirWatcher: path:Path -> Async<IWatcher>

type private DiskWatcher(path:Path) = 
    let w = new FileSystemWatcher(path)
    let evt = w.Created |> Event.map(fun x -> x.FullPath)
    do w.EnableRaisingEvents <- true
    interface IWatcher with
        member __.Credated = evt
        member __.Dispose() = w.Dispose()
    
type Disk() =
    interface Interface with
        member __.ListFiles(path) = async {
            return Directory.GetFiles path |> List.ofArray
        }
        member __.OpenRead(path) = File.OpenRead path :> Stream
        member __.OpenWrite(path) = File.OpenWrite path :> Stream
        member __.DirWatcher(path) = async { return new DiskWatcher(path) :> IWatcher}

type Installer() =
    interface IWindsorInstaller with
        member __.Install(c, _) = 
            c.Register 
                (Component.For<Interface>().ImplementedBy<Disk>()) 
            |> ignore


