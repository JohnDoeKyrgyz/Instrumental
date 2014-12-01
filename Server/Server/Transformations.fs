namespace Instrumental
module Transformations =
    open System
    open System.Threading.Tasks
    open System.Reactive.Concurrency
    open FSharp.Control.Reactive
    open Protocol
    open System.Threading
    open System.Reactive.Subjects
    
    let trackGreatestTime data =
        let readTimePart data = readTime data, data
        let greatestTime (prev, _) (time, data) =
            let nextTime = if prev > time then time else prev
            nextTime, (time, data)
        data
        |> Observable.map readTimePart
        |> Observable.scanInit greatestTime (0L, (0L, Array.empty<byte>))

    let newest data =        
        let mostRecent (prev, (time, _)) = time >= prev
        data
        |> Observable.filter mostRecent
        |> Observable.map snd

    let readSensor data =
        let readSensorPart (time, data) =
            let sensor = readSensor data
            time, sensor, data
        data
        |> Observable.map readSensorPart

    let filterForSensors sensors data =        
        let isValidSensor (_, sensor, _) =
            sensors
            |> Set.contains sensor
        data
        |> Observable.filter isValidSensor

    let readValues data =
        let readValuesPart (time, sensor, data) =
            let values = readValues data
            time, sensor, values
        data
        |> Observable.map readValuesPart

    let createReading data =
        let buildReading (time, sensor, values) = {
            Time = time
            Sensor = sensor
            Values = values }
        data
        |> Observable.map buildReading

    let createMessages readings =
        readings
        |> trackGreatestTime
        |> newest
        |> readSensor
        |> readValues
        |> createReading

    type Reading<'a> =
        | Resume
        | Value of 'a
        | Timeout

    /// Indicates if a reading is for a Value.
    let isValue v =
        match v with
        | Value _ -> true
        | _ -> false

    /// Gets the value of a reading. If v is not a Value an exception is thrown.
    let value v =
        match v with
        | Value v -> v
        | v -> failwith (sprintf "Unexpected value %A" v)

    /// Watch a stream of values with a timeout. The resulting stream is wrapped with a Reading.
    let withTimeout (timeout : TimeSpan) values = 

        let lastEventTime = ref 0L
        let timeoutReset = new ManualResetEvent(false)
        let inTimeout = ref 0
        let completed = ref false

        let timeoutSignal = new Subject<Reading<'a>>()
        
        let onValue value =
            lastEventTime := DateTime.Now.Ticks
            if timeoutReset.Set() && Interlocked.CompareExchange(inTimeout, 0, 1) = 1 then do            
                timeoutSignal.OnNext Resume            
            Value value

        let values = 
            values 
            |> Observable.map onValue
            |> Observable.finallyDo (fun () -> completed := true)

        let checkForTimeout = Task.Factory.StartNew( (fun () ->
            while not !completed do
                if timeoutReset.WaitOne() then do
                    let timeoutMillis = int64 timeout.TotalMilliseconds
                    Thread.Sleep (int (timeoutMillis / 2L))
                    let currentTime = DateTime.Now.Ticks
                    let timeSinceLastEvent = (currentTime - !lastEventTime) / TimeSpan.TicksPerMillisecond
                    if timeSinceLastEvent > timeoutMillis && timeoutReset.Reset() then do
                        inTimeout := 1
                        timeoutSignal.OnNext Timeout), TaskCreationOptions.LongRunning )

        timeoutSignal
        |> Observable.merge values