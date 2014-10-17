namespace System
    module Observable =
        open System
        open System.Threading

        /// Creates an observable that calls the specified function after someone
        /// subscribes to it (useful for waiting using 'let!' when we need to start
        /// operation after 'let!' attaches handler)
        let guard f (e:IObservable<'Args>) =  
          { new IObservable<'Args> with  
              member x.Subscribe(observer) =  
                let rm = e.Subscribe(observer) in f(); rm }

        let ofSeq s = 
            let evt = new Event<_>()

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
                async{ s |> Seq.iter raiseEventOnMainThread }
                |> Async.Start
                            
            evt.Publish 
            |> guard consumeSequence

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

        let window size observable =
            let fillWindow window value =
                if List.length window = size then [value]
                else value :: (window |> Seq.take (size - 1) |> Seq.toList)

            observable
            |> Observable.scan fillWindow []
            |> Observable.filter (fun window -> List.length window = size)