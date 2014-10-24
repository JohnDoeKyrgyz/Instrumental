namespace Instrumental

    module Summary =
        open System

        open FSharp.Control.Reactive

        open Instrumental.Listener
        open Instrumental.Transformations
        open Instrumental.Protocol

        /// Cummulative summary information for a single sensor
        type SensorSummary = {
            MinX: float32
            MaxX: float32
            MinY: float32
            MaxY: float32
            MinZ: float32
            MaxZ: float32
            MinXTime: DateTime
            MaxXTime: DateTime
            MinYTime: DateTime
            MaxYTime: DateTime
            MinZTime: DateTime
            MaxZTime: DateTime }
            with
                override this.ToString() = 
                    [ this.MinX, this.MinXTime
                      this.MaxX, this.MaxXTime
                      this.MinY, this.MinYTime
                      this.MaxY, this.MaxYTime
                      this.MinZ, this.MinZTime
                      this.MaxZ, this.MaxZTime ]
                    |> Seq.map (fun (v, time) -> sprintf "%-8f %s" v (time.ToShortTimeString()))
                    |> String.concat ", "

        /// Summary information for a single user
        type Summary = {
            Name: string
            Sensors : Map<int, SensorSummary> }
            with
                override this.ToString() =
                    let sensorLines =
                        this.Sensors
                        |> Seq.map (fun entry -> sprintf "%-2d %O" entry.Key entry.Value)
                        |> String.concat Environment.NewLine
                    this.Name + Environment.NewLine + sensorLines

        let onError (ex : exn) =
            printfn "ERROR: %s" ex.Message

        let printAllReadings() =
            let readSenderStream (sender, data) =
                createMessages data
                |> Observable.map (fun value -> sprintf "%s %i %O" sender (System.Threading.Thread.CurrentThread.GetHashCode()) value)

            readingsBySource
            |> Observable.flatmap readSenderStream
            |> Observable.subscribeWithError (Console.Out.WriteLine) onError

        let readingsSummary() =
            let initialSummary =
                let time = DateTime.Now
                { MinX = 0.0f
                  MaxX = 0.0f
                  MinY = 0.0f 
                  MaxY = 0.0f
                  MinZ = 0.0f
                  MaxZ = 0.0f
                  MinXTime = time
                  MaxXTime = time
                  MinYTime = time
                  MaxYTime = time
                  MinZTime = time
                  MaxZTime = time }

            let updateSummary summary reading =
                
                let get index =
                    if index < reading.Values.Length
                    then Some reading.Values.[index]
                    else None
                
                let newTime = DateTime.Now

                let x = get 0
                let y = get 1
                let z = get 2

                let pick existing existingTime read comparitor =
                    match read with
                    | None -> existing, existingTime
                    | Some value ->
                        let newValue = comparitor value existing
                        if value = newValue 
                        then newValue, existingTime
                        else newValue, newTime

                let newMinX, newMinXTime = pick summary.MinX summary.MinXTime x min
                let newMaxX, newMaxXTime = pick summary.MaxX summary.MaxXTime x max
                let newMinY, newMinYTime = pick summary.MinY summary.MinYTime y min
                let newMaxY, newMaxYTime = pick summary.MaxY summary.MaxYTime y max
                let newMinZ, newMinZTime = pick summary.MinZ summary.MinZTime z min
                let newMaxZ, newMaxZTime = pick summary.MaxZ summary.MaxZTime z max

                { MinX = newMinX
                  MaxX = newMaxX
                  MinY = newMinY 
                  MaxY = newMaxY
                  MinZ = newMinZ
                  MaxZ = newMaxZ
                  MinXTime = newMinXTime
                  MaxXTime = newMaxXTime
                  MinYTime = newMinYTime
                  MaxYTime = newMaxYTime
                  MinZTime = newMinZTime
                  MaxZTime = newMaxZTime }
                
            let summarize summaries reading =
                let existingSummary =
                    match summaries |> Map.tryFind reading.Sensor with
                    | Some summary -> summary
                    | None -> initialSummary
                let nextSummary = updateSummary existingSummary reading
                summaries |> Map.add reading.Sensor nextSummary

            let summarizeSource (name, values) =
                values
                |> Transformations.createMessages
                |> Observable.scanInit summarize Map.empty
                |> Observable.map (fun sensorSummaries -> {Name = name; Sensors = sensorSummaries})

            readingsBySource
            |> Observable.flatmap summarizeSource

        let printSummary() =
            readingsSummary()
            |> Observable.subscribeWithError (printfn "%O") onError