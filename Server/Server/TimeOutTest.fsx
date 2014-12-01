#load "Instrumental.fsx"

open System
open System.Threading.Tasks
open System.Threading
open System.Net
open System.Reactive.Subjects
open System.Reactive.Linq
open System.Reactive.Concurrency
open FSharp.Control.Reactive

type Reading<'a> =
    | Connect
    | Value of 'a
    | Timeout

let readings =
    seq { for i in 1 .. Int32.MaxValue do
            let delay = if i % 20 = 0 then 2000 else 0
            Thread.Sleep(delay)                
            yield i }
    |> Observable.ToObservable

let onNext = printfn "%A"
let onError = printfn "%A"
let onCompleted() = printfn "Done"

let timeout = 200L

let rec withTimeout timeout values = 

    let lastEventTime = ref 0L
    let timeoutReset = new ManualResetEvent(false)
    let inTimeout = ref 0

    let timeoutSignal = new Subject<Reading<'a>>()

    let onValue value =
        lastEventTime := DateTime.Now.Ticks
        if timeoutReset.Set() && Interlocked.CompareExchange(inTimeout, 0, 1) = 1 then do            
            timeoutSignal.OnNext Connect            
        Value value

    let values = values |> Observable.map onValue

    let checkForTimeout = Task.Factory.StartNew( (fun () ->
        while timeoutSignal.HasObservers do
            if timeoutReset.WaitOne() then do           
                Thread.Sleep ((int timeout) / 2)
                let currentTime = DateTime.Now.Ticks
                let timeSinceLastEvent = (currentTime - !lastEventTime) / TimeSpan.TicksPerMillisecond
                if timeSinceLastEvent > timeout && timeoutReset.Reset() then do
                    inTimeout := 1
                    timeoutSignal.OnNext Timeout), TaskCreationOptions.LongRunning )

    timeoutSignal
    //|> Observable.observeOnContext (SynchronizationContext.Current)
    |> Observable.merge values

let start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond

readings
|> withTimeout timeout
|> Observable.map (fun v -> v, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - start)
|> Observable.take 100
|> Observable.subscribeWithCallbacks onNext onError onCompleted
