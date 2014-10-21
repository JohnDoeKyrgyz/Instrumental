namespace Instrumental

    module Main =        
        open System
        open System.Net
        
        open Protocol
                
        /// Groups raw readings the address of the sender
        let readingsBySource errorHandler =

            let readAddress (endpoint : IPEndPoint, _) = endpoint.Address.ToString()

            let data, errors =
                Listener.messages
                |> Observable.ofSeq
                |> Observable.partitionChoice

            errors
            |> Observable.add errorHandler

            data
            |> Observable.partitionBy readAddress
            |> Observable.map (fun (address, partitionedObservable) -> address, partitionedObservable |> Observable.map snd)

        /// Locks on to the stream of data from the first sender in a group
        let singleSource (data : IObservable<'TKey * IObservable<'TValue>>) =
            let result = new Event<'TValue>()
            let increment (count, _) value = count + 1, value

            data
            |> Observable.scan increment (0, Unchecked.defaultof<_>)
            |> Observable.filter (fun (i, _) -> i = 1)
            |> Observable.map snd
            |> Observable.add (fun (_, vs) -> vs |> Observable.add (result.Trigger))

            result.Publish :> IObservable<'TValue>

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

            readingsBySource onError
            |> Observable.add readSenderStream                               

        [<EntryPoint>]
        let main _ =
            printfn "Listening for sensor messages %A" (System.Threading.SynchronizationContext.Current)
                        
            // Test 1) Read all incomming data
            //printAllReadings()

            // Test 2) Read data from first source
            let firstSource =
                readingsBySource onError
                |> singleSource

            firstSource
            |> Observable.add (fun v -> printfn "%A" v)

            printfn "Press any key to exit..."
            Console.ReadKey() |> ignore

            0 // return an integer exit code
