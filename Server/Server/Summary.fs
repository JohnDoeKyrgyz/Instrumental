namespace Instrumental

    module Summary =
        
        open System
        open System.Reactive.Subjects
        open System.Net.Sockets

        open FSharp.Control.Reactive

        open Instrumental.Protocol
        open Instrumental.Listener

        type ValueSummary = {
            Value: float32
            Time: DateTime
            DeviceTime: int64 }
            with
                static member Default = {Value = 0.0f; Time = DateTime.MinValue; DeviceTime = 0L}

        type ValueUpdate =
            | Min of ValueSummary
            | Max of ValueSummary

        type Update = {
            Device: string
            Sensor: int
            Update: ValueUpdate
            Index: int }

        type UpdateMessage =
            | Update of Update
            | Connect of string
            | Disconnect of string

        type ResetMessage =
            | ResetAll of string
            | ResetSensor of string * int

        type private ReadingSummary = {
            Min: ValueSummary
            Max: ValueSummary }

        let summarizeReadings deviceTimeout (resetSignal : IObservable<ResetMessage>) =

            let newTime = DateTime.Now

            let createValue value time deviceTime = {Value = value; Time = time; DeviceTime = deviceTime}

            let initializeValueSummary deviceTime value =
                let valueSummary = createValue value newTime deviceTime
                {Min = valueSummary; Max = valueSummary}
                
            let updateValue value deviceTime comparitor existingValue =
                let newValue = comparitor (existingValue.Value) value
                if value = newValue then existingValue else createValue value newTime deviceTime

            let addReading key readings reading =
                let update comparitor i = updateValue reading.Values.[i] reading.Time comparitor
                let updateReadingSummary i readingSummary =
                    let updateMin = update min
                    let updateMax = update max
                    {Min = updateMin i readingSummary.Min; Max = updateMax i readingSummary.Max}
                
                let newSummaries, oldSummaries =
                    match Map.tryFind reading.Sensor readings with
                    | Some existingSummary ->
                        let newSummary =
                            existingSummary
                            |> List.mapi updateReadingSummary
                        newSummary, Some existingSummary
                    | None -> 
                        let newSummary = [for value in reading.Values -> initializeValueSummary reading.Time value]
                        newSummary, None

                let updates =
                    match oldSummaries with
                    | None -> []
                    | Some oldSummaries ->
                        [for (i, oldSummary, newSummary) in List.zip3 [0 .. (List.length oldSummaries) - 1] newSummaries oldSummaries do
                            if oldSummary <> newSummary then
                                let valueUpdate =
                                    if oldSummary.Max < newSummary.Max 
                                    then Max newSummary.Max
                                    else Min newSummary.Min
                                yield {Device = key; Sensor = reading.Sensor; Update = valueUpdate; Index = i}]
                                                        
                let readings = 
                    readings 
                    |> Map.add reading.Sensor newSummaries

                readings, updates

            let applyReset reset key readings =
                let resetKey, builder =
                    match reset with
                    | ResetAll resetKey -> 
                        resetKey, fun () -> Map.empty
                    | ResetSensor( resetKey, sensor) -> 
                        resetKey, fun () -> if readings |> Map.containsKey sensor then readings |> Map.remove sensor else readings
                if resetKey = key then builder() else readings                    

            let processMessages key (readings, _ : Update list) message =
                match message with
                | Choice1Of2 reading -> addReading key readings reading
                | Choice2Of2 reset -> applyReset reset key readings, []

            let summarizeDeviceReadings key readings =
                            
                let resetSignal =
                    resetSignal
                    |> Observable.map Choice2Of2

                let readingMessages =
                    readings
                    |> Transformations.createMessages
                    |> Observable.map Choice1Of2

                readingMessages
                |> Observable.merge resetSignal                
                |> Observable.scanInit (processMessages key) (Map.empty, [])
                |> Observable.map snd
                |> Observable.filter (List.isEmpty >> not)
                |> Observable.flatmapSeq List.toSeq
                |> Observable.map Update       

            let createClientUpdates (key, (values : IObservable<byte[]>)) =
                
                let timmedValues =
                    values
                    |> Transformations.withTimeout deviceTimeout
                                    
                let summarizedReadings =
                    timmedValues
                    |> Observable.filter Transformations.isValue
                    |> Observable.map Transformations.value
                    |> summarizeDeviceReadings key                
                
                timmedValues
                |> Observable.filter (Transformations.isValue >> not)
                |> Observable.map (fun v ->
                    match v with
                    | Transformations.Resume -> Connect key
                    | Transformations.Timeout -> Disconnect key
                    | _ -> failwith "Unexpected value" )
                |> Observable.merge summarizedReadings             
                                
            readingsBySource
            |> Observable.flatmap createClientUpdates