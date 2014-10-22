namespace Instrumental

    module Main =        
        open System
        open System.Net
        open FSharp.Control.Reactive
        
        open Protocol
        open System.Reactive.Concurrency
                
        /// Groups raw readings the address of the sender
        let readingsBySource =

            let readAddress (endpoint : IPEndPoint, _) = endpoint.Address.ToString()

            let data =
                Listener.messages
                |> Observable.ofSeqOn Scheduler.Default

            data
            |> Observable.groupBy readAddress
            |> Observable.map (fun group -> group.Key, group |> Observable.map snd)

        /// Locks on to the stream of data from the first sender in a group
        let singleSource (data : IObservable<'TKey * IObservable<'TValue>>) =
            data
            |> Observable.first
            |> Observable.flatmap snd

        let createMessages readings =
            let timmed =
                readings
                |> Transformations.trackGreatestTime

            let newer = timmed |> Transformations.newest
            let older = timmed |> Transformations.older                

            let messages =
                newer
                |> Observable.map (fun (time, data) -> time, readSensor data, data)
                |> Transformations.readValues
                |> Transformations.createReading
            messages, older
            
        let onError (ex : exn) =
            printfn "ERROR: %s" ex.Message

        let printAllReadings() =
            let readSenderStream (sender, data) =
                let messages, older = createMessages data
                Observable.add (fun value -> printfn "%s %i %O" sender (System.Threading.Thread.CurrentThread.GetHashCode()) value) messages
                Observable.add (fun _ -> Console.Beep(440, 10)) older

            readingsBySource
            |> Observable.subscribeWithError readSenderStream onError
            |> ignore