module FSnack.Candies.Service.Events
open FSnack.CoreLib
open Castle.MicroKernel.Registration
open System
open System.IO

type NewFileEvent = NewFileEvent of FileSystem.Path

type NewFileListener(fs:FileSystem.Interface, dispatcher:App.IDispatcher<NewFileEvent>) =
    let candyDir =
        System.IO.Path.Combine [|
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            "Candies"
        |]

    interface App.IStartable with
        member __.OrdinalNumber = 0
        member __.Startup = async {
            if not(Directory.Exists candyDir) 
            then Directory.CreateDirectory candyDir |> ignore

            let! w = fs.DirWatcher candyDir
            w.Credated.Add(NewFileEvent >> dispatcher.Post)
        }

type Installer() =
    interface IWindsorInstaller with
        member __.Install(c, _) = 
            c.Register(
                Component
                    .For<App.IStartable>()
                    .ImplementedBy<NewFileListener>())
            |> ignore