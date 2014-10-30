#load "Instrumental.fsx"

open Instrumental.Listener
open System.Net

let localEndpoint = ref (IPEndPoint(IPAddress.Any, 5000))
let data = client.Receive(localEndpoint)

printfn "Communication is working! %A" data
