namespace Instrumental

    module Summary =
        
        open System

        open FSharp.Control.Reactive

        open Instrumental.Protocol
        open Instrumental.Listener

        type ValueSummary = {
            Value: float32
            Time: DateTime
            DeviceTime: int64 }

        type ReadingSummary = {
            Min: ValueSummary
            Max: ValueSummary }

        type SensorSummary = {
            Number: int
            Readings: ReadingSummary array }

        type DeviceSummary = {
            Name: string
            Sensors: Map<int, SensorSummary> }

        type ClientListAction<'TValue> =
            | Add of string * IObservable<'TValue>
            | Remove of string

        let summarizeReadings deviceTimeout =

            let newTime = DateTime.Now

            let createValue value time deviceTime = {Value = value; Time = time; DeviceTime = deviceTime}

            let initializeValueSummary deviceTime value =
                let valueSummary = createValue value newTime deviceTime
                {Min = valueSummary; Max = valueSummary}
                
            let updateValue value deviceTime comparitor existingValue =
                let newValue = comparitor (existingValue.Value) value
                if value = newValue then existingValue else createValue value newTime deviceTime

            let addReading readings reading =
                let update comparitor i = updateValue reading.Values.[i] reading.Time comparitor
                let updateReadingSummary i readingSummary =
                    let updateMin = update min
                    let updateMax = update max
                    {Min = updateMin i readingSummary.Min; Max = updateMax i readingSummary.Max}
                
                let readingSummary =
                    match Map.tryFind reading.Sensor readings with
                    | Some existingSummary ->
                        { Number = reading.Sensor
                          Readings = existingSummary.Readings |> Array.mapi updateReadingSummary }
                    | None -> 
                        { Number = reading.Sensor
                          Readings = reading.Values |> Array.map (initializeValueSummary reading.Time)}
                readings |> Map.add reading.Sensor readingSummary

            let summarizeDeviceReadings key readings =
                let makeDeviceSummary sensorSummary = {Name = key; Sensors = sensorSummary}
                readings
                |> Transformations.createMessages
                |> Observable.scanInit addReading Map.empty
                |> Observable.map makeDeviceSummary

            let createClientListActions (key, values) =
                    
                let observeTimeout (observer : IObserver<ClientListAction<'TValue>>) =
                    let onError (error : exn) = 
                        match error with
                        | :? TimeoutException -> observer.OnNext( Remove key )
                        | otherError -> observer.OnError( otherError )
                    values
                    |> Observable.timeoutSpan deviceTimeout
                    |> Observable.subscribeWithError ignore onError

                let timeout = Observable.createWithDisposable(observeTimeout)
                let valueAdd = Observable.single (Add (key, values))
                valueAdd |> Observable.concat timeout
                
            let processClientListActions devices action =
                match action with
                | Remove key -> devices |> Map.remove key
                | Add(key, values) -> 
                    let deviceSummaries = summarizeDeviceReadings key values
                    devices |> Map.add key deviceSummaries

            readingsBySource
            |> Observable.flatmap createClientListActions
            |> Observable.scanInit processClientListActions Map.empty