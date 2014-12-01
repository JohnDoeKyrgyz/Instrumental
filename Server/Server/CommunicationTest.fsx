#load "Instrumental.fsx"

open Instrumental.Listener
open System
open System.Net
open System.Reactive.Linq
open System.Reactive.Disposables

let localEndpoint = ref (IPEndPoint(IPAddress.Any, 5000))
let data = client.Receive(localEndpoint)

printfn "Communication is working! %A" data

open Instrumental.Summary

let summary =     
    summarizeReadings (TimeSpan.FromMilliseconds 1000.0)
    |> (Observable.subscribe (printfn "%A"))
