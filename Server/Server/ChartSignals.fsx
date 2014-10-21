#I @"..\packages\FSharp.Charting.0.90.7"
#load "FSharp.Charting.fsx"
#r @"bin\Debug\InstrumentalServer.exe"

open System
open System.Drawing

open FSharp.Charting

open Instrumental
open Instrumental.Main

//printAllReadings()

let readings =
    readingsBySource onError
    |> singleSource 
    |> Transformations.trackGreatestTime
    |> Transformations.newest
    |> Transformations.readSensor

let byTime values =
    values
    |> Observable.map (fun (time, _, values) -> time, values)

let threeAxisReadings timmed =    

    let select index (time, values : array<_>) = time, values.[index]

    let xs = timmed |> Observable.map (select 0)
    let ys = timmed |> Observable.map (select 1)
    let zs = timmed |> Observable.map (select 2)

    xs, ys, zs

let sensor id =
    readings
    |> Transformations.filterForSensors (set [id])
    |> Transformations.readValues
    |> byTime

let acclerometer = sensor 10 |> threeAxisReadings 
let gyro = sensor 4 |> threeAxisReadings
let rotation = sensor 15 |> threeAxisReadings
let magnet = sensor 2 |> threeAxisReadings
let touch = 
    sensor 69
    |> Observable.map (fun (_, [|x;y|]) -> x, -y)

let smooth (factor : float32) data =
    data
    |> Observable.pairwise
    |> Observable.filter (fun ((_, fy), (_, sy)) -> abs(fy - sy) > factor)
    |> Observable.map snd

let graphThreeValueSensor (xs, ys, zs) smoothing title =

    let xs = xs |> smooth smoothing
    let ys = ys |> smooth smoothing
    let zs = zs |> smooth smoothing
    let form =
        Chart
            .Combine([
                        LiveChart.FastLineIncremental(xs, "x", Color=Color.Black)
                        LiveChart.FastLineIncremental(ys, "y", Color=Color.Red)
                        LiveChart.FastLineIncremental(zs, "z", Color=Color.Green) ])
            .WithTitle(title)
            .ShowChart()
    form.Text <- title

let graphXYSensor values title =
    let form = LiveChart.FastLineIncremental(values, Title = title).ShowChart()
    form.Text <- title

graphThreeValueSensor acclerometer 0.1f "Acceleration"
graphThreeValueSensor gyro 0.1f "Gyro"
graphThreeValueSensor rotation 0.0001f "Rotation"
graphThreeValueSensor magnet 0.0001f "Magnet"
graphXYSensor touch "Touch"

readings
|> Observable.scan (fun sensors (_, sensor, _) -> (Set.add sensor sensors)) (Set.empty)
|> Observable.pairwise
|> Observable.filter (fun (a, b) -> a <> b)
|> Observable.map snd
|> Observable.add (printfn "%A")


printfn "Launched Chart"