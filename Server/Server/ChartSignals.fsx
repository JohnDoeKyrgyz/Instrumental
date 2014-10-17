#I @"..\packages\FSharp.Charting.0.90.7"
#load "FSharp.Charting.fsx"
#r @"bin\Debug\InstrumentalServer.exe"

open System
open System.Drawing

open FSharp.Charting

open Instrumental
open Instrumental.Main

printAllReadings()

let readings =
    readingsBySource 
    |> singleSource
    |> Transformations.trackGreatestTime
    |> Transformations.newest
    |> Transformations.readSensor

let touch =
    readings
    |> Transformations.filterForSensors (set [69])
    |> Transformations.readValues
    |> Observable.map (fun (_, _, [|x;y|]) -> x, y)

let acclerometer =
    readings
    |> Transformations.filterForSensors (set [10])
    |> Transformations.readValues   

let byTime selector values =
    values
    |> Observable.map (fun (time, _, values) -> time, selector values)

let smooth (factor : float32) data =
    data
    |> Observable.pairwise
    |> Observable.filter (fun ((_, fy), (_, sy)) -> abs(fy - sy) > factor)
    |> Observable.map snd

let xs = acclerometer |> byTime (fun values -> values.[0]) |> smooth 0.05f
let ys = acclerometer |> byTime (fun values -> values.[1]) |> smooth 0.05f
let zs = acclerometer |> byTime (fun values -> values.[2]) |> smooth 0.05f

//LiveChart.FastLine(xs |> Observable.window 1000).ShowChart()
xs
|> Observable.subscribe (fun (time, value) -> printfn "%d %f" time value)

Chart
    .Combine([
                LiveChart.FastLineIncremental(xs, "x", Color=Color.Black)
                LiveChart.FastLineIncremental(ys, "y", Color=Color.Red)
                LiveChart.FastLineIncremental(zs, "z", Color=Color.Green) ])
    .ShowChart()

printfn "Launched Chart"