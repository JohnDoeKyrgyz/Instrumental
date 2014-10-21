namespace System
    module Observable =
        open System
        open System.Collections.Generic
        open System.Threading
        open System.Timers

        /// Creates an observable that calls the specified function after someone
        /// subscribes to it (useful for waiting using 'let!' when we need to start
        /// operation after 'let!' attaches handler)
        let guard f (e:IObservable<'Args>) =  
          { new IObservable<'Args> with  
              member x.Subscribe(observer) =  
                let rm = e.Subscribe(observer) in f(); rm }
        
        /// Creates an observable from a sequence. The sequence is consumed asynchronously, with all
        /// messages being written to the observable on the main thread. The resulting observable
        /// allows you to observe values, as well as any exceptions that result from processing the sequence.
        let ofSeq s = 
            let evt = new Event<Choice<_, exn>>()

            let mainThreadContext = 
                let context = SynchronizationContext.Current
                if context = null then
                    let newContext = new SynchronizationContext()
                    SynchronizationContext.SetSynchronizationContext(newContext)
                    newContext
                else context                
            
            let raiseEventOnMainThread data = 
                async {
                    do! Async.SwitchToContext mainThreadContext                    
                    evt.Trigger data }
                |> Async.RunSynchronously

            let consumeSequence() =
                async{ 
                    try
                        s 
                        |> Seq.map Choice1Of2 
                        |> Seq.iter raiseEventOnMainThread
                    with ex ->
                        raiseEventOnMainThread (Choice2Of2 ex) }
                |> Async.Start
                            
            evt.Publish 
            |> guard consumeSequence

        /// Takes an observable of Choice1Of2 and creates an observable for the first side of the choice and another for the second side.
        let partitionChoice signal =

            let isValue value =
                match value with
                | Choice1Of2 _ -> true
                | Choice2Of2 _ -> false

            let getValue value =
                match value with
                | Choice1Of2 value -> value
                | _ -> failwith "Errors should be filtered out"

            let getException value =
                match value with
                | Choice2Of2 exn -> exn
                | _ -> failwith "Errors should be filtered out"

            let values, errors =
                signal
                |> Observable.partition isValue

            values |> Observable.map getValue, errors |> Observable.map getException

        /// Similar to partitionBy, but all groups are provided in a map
        let groupBy keyBuilder observable = 
            let groupByKey (groups : Map<'TKey, Event<'TValue>>) value =
                let key = keyBuilder value
                let observableForKey, groups =
                    match Map.tryFind key groups with
                    | Some v -> v, groups
                    | None ->
                        let keyObservable = new Event<'TValue>()
                        keyObservable, Map.add key keyObservable groups

                observableForKey.Trigger(value)

                groups
                
            observable
            |> Observable.scan groupByKey Map.empty
            |> Observable.map (Map.map (fun _ v -> v.Publish :> IObservable<'TValue>))

        /// Partitions an observable by key, into an observable of key and observable.
        let partitionBy (keyBuilder : 'V -> 'K) observable =

            let mapDifference (_, prev) current = 
                let newKeys =
                    [for key, _ in current |> Map.toSeq do
                        if not (Map.containsKey key prev) then
                            yield key]
                let newKey =
                    match newKeys with
                    | key :: _ -> Some key
                    | [] -> None                    
                newKey, current

            let pick (key, groups) =
                let key = Option.get key
                let partitionedObservable = Map.find key groups
                key, partitionedObservable
                            
            observable
            |> groupBy keyBuilder
            |> Observable.scan mapDifference (None, Map.empty)
            |> Observable.filter (fst >> Option.isSome)
            |> Observable.map pick

        /// Creates a sliding window of <size> values. The window will not be reported until it is full, then it will be reported for every event after that.
        let window size observable =
            let fillWindow (windowBuilder : Queue<_>, _) value =
                windowBuilder.Enqueue(value)
                let resultWindow =
                    if windowBuilder.Count = size then                         
                        let result = Some (windowBuilder.ToArray())
                        windowBuilder.Dequeue() |> ignore
                        result
                    else None
                windowBuilder, resultWindow

            observable
            |> Observable.scan fillWindow (new Queue<_>(), None)
            |> Observable.filter (snd >> Option.isSome)
            |> Observable.map snd