#r @"..\packages\Rx-Core.2.2.5\lib\net40\System.Reactive.Core.dll"
#r @"..\packages\Rx-Linq.2.2.5\lib\net40\System.Reactive.Linq.dll"
#r @"..\packages\Rx-Interfaces.2.2.5\lib\net40\System.Reactive.Interfaces.dll"
#r @"..\packages\Rx-PlatformServices.2.2.5\lib\net45\System.Reactive.PlatformServices.dll"

#load "Observable.fs"

open FSharp.Control.Reactive
open System
open System.Threading
open System.Reactive.Concurrency

let randoms = seq {
    let random = new Random()
    while (true) do
        yield random.Next() }

let printThread _ = printfn "%d" (Thread.CurrentThread.GetHashCode())

let mainContext = SynchronizationContext.Current

randoms
|> Observable.ofSeqOn Scheduler.Default
|> Observable.iter printThread
|> Observable.observeOnContext mainContext
|> Observable.take 10
|> Observable.add (printfn "%A")