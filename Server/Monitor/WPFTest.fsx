#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml"

#r @"..\packages\Rx-Core.2.2.5\lib\net40\System.Reactive.Core.dll"
#r @"..\packages\Rx-Linq.2.2.5\lib\net40\System.Reactive.Linq.dll"
#r @"..\packages\Rx-Interfaces.2.2.5\lib\net40\System.Reactive.Interfaces.dll"
#r @"..\packages\Rx-PlatformServices.2.2.5\lib\net45\System.Reactive.PlatformServices.dll"
#r @"..\packages\Rx-XAML.2.2.5\lib\net45\System.Reactive.Windows.Threading.dll"

#r @"bin\Debug\InstrumentalServer.dll"

open System
open System.Threading
open System.Windows
open System.Windows.Threading
open System.Windows.Controls

open System.Reactive.Concurrency

open Instrumental
open Instrumental.Listener

open FSharp.Control.Reactive

let w = Window(Title = "Test")

let timeout = TimeSpan.FromMilliseconds(200.0)
Summary.summarizeReadings timeout
|> Observable.observeOn (DispatcherScheduler.Current)
|> Observable.subscribe (fun v -> w.Title <- string v)

(*
Summary.summarizeReadings 100
|> Observable.observeOn (DispatcherScheduler.Current)
|> Observable.subscribe (fun v -> w.Title <- string v)
*)

let application = new Application()
application.Run(w)