module FSnack.CoreLib.FileSystem

open System.IO
open Castle.MicroKernel.Registration
open Castle.Core.Logging

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
    abstract AssureDir: path:Path -> Async<unit>

type private DiskWatcher(path:Path) = 
    let w = new FileSystemWatcher(path)
    let evt = w.Created |> Event.map(fun x -> x.FullPath)
    do w.EnableRaisingEvents <- true
    interface IWatcher with
        member __.Credated = evt
        member __.Dispose() = w.Dispose()
    
type Disk(logger:ILogger) =
    interface Interface with
        member __.ListFiles(path) = async {
            return Directory.GetFiles path |> List.ofArray
        }
        member __.OpenRead(path) = 
            logger.Infof "opening file %s for read" path
            File.OpenRead path :> Stream
        member __.OpenWrite(path) = 
            logger.Infof "opening file %s for write" path
            File.OpenWrite path :> Stream
        member __.DirWatcher(path) = async { return new DiskWatcher(path) :> IWatcher}
        member __.AssureDir(path) = async {
            if not (Directory.Exists path )
            then
                logger.Infof "Creating directory %s" path
                Directory.CreateDirectory path |> ignore
        }

type Installer() =
    interface IWindsorInstaller with
        member __.Install(c, _) = 
            c.Register 
                (Component.For<Interface>().ImplementedBy<Disk>()) 
            |> ignore


