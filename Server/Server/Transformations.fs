namespace Instrumental
module Transformations =
    open Protocol

    let trackGreatestTime data =
        let readTimePart data = readTime data, data
        let greatestTime (prev, _) (time, data) =
            let nextTime = if prev > time then time else prev
            nextTime, (time, data)
        data
        |> Observable.map readTimePart
        |> Observable.scan greatestTime (0L, (0L, Array.empty<byte>))

    let newest data =        
        let mostRecent (prev, (time, _)) = time >= prev
        data
        |> Observable.filter mostRecent
        |> Observable.map snd

    let older data =
        let older (prev, (time, _)) = time < prev            
        data
        |> Observable.filter older
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