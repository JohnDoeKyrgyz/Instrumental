namespace Instrumental.ViewModel

open System.Collections.ObjectModel

open Instrumental.Summary

type ValueSummaryModel() =
    inherit ViewModelBase()
    with
        let min = ref ValueSummary.Default        
        member this.Min
            with get() = !min
            and set value = setProperty this <@@ this.Min @@> min value

        let max = ref ValueSummary.Default        
        member this.Max
            with get() = !max
            and set value = setProperty this <@@ this.Max @@> max value

type SensorSummaryModel(sensor : int) =
    inherit ViewModelBase()
    with
        member this.Sensor = sensor
        member this.Values = new ObservableCollection<ValueSummaryModel>([for i in 0 .. 3 -> new ValueSummaryModel()])

type DeviceSummaryModel(name : string) =
    inherit ViewModelBase()
    with
        member this.Name = name
        member this.Sensors = new ObservableCollection<SensorSummaryModel>()

module SummaryTranslation =

    open System
    open System.Reactive.Concurrency
    open FSharp.Control.Reactive

    let subscribeToSummaryUpdates timeout (devices : ObservableCollection<DeviceSummaryModel>) =
        
        let index = 
            seq {
                for device in devices do
                    let sensorsByNumber =
                        seq {
                            for sensor in device.Sensors do
                                yield sensor.Sensor, sensor}
                            |> Map.ofSeq
                    yield device.Name, (device, sensorsByNumber)}
            |> Map.ofSeq

        let processUpdate (index : Map<string, DeviceSummaryModel * Map<int, SensorSummaryModel>>) updateMessage =
            
            match updateMessage with
            | Disconnect device ->
                match index |> Map.tryFind device with
                | Some (model, sensorIndex) ->
                    devices.Remove(model) |> ignore
                    index |> Map.remove device
                | None -> index

            | Connect device ->
                match index |> Map.tryFind device with
                | None ->
                    let model = DeviceSummaryModel device
                    devices.Add model 
                    let sensorIndex = Map.empty<int, SensorSummaryModel>
                    index |> Map.add device (model, Map.empty) 
                | Some _ -> index

            | Update update ->

                //find or add device
                let (deviceSummary, sensorsIndex) =
                    match index |> Map.tryFind update.Device with
                    | Some entry -> entry
                    | None ->
                        let deviceSummary = new DeviceSummaryModel(update.Device)
                        devices.Add(deviceSummary)
                        (deviceSummary, Map.empty)

                //find or add sensor
                let sensorIndex, sensorSummary =
                    match sensorsIndex |> Map.tryFind update.Sensor with
                    | Some sensorSummary -> sensorsIndex, sensorSummary
                    | None ->
                        let sensorSummary = new SensorSummaryModel(update.Sensor)
                        deviceSummary.Sensors.Add(sensorSummary)

                        let sensorIndex =
                            sensorsIndex 
                            |> Map.add (update.Sensor) sensorSummary

                        sensorIndex, sensorSummary

                //find or add value summary
                let valuesCount = sensorSummary.Values.Count
                if update.Index > valuesCount then do
                    for i in valuesCount .. (update.Index - 1) do
                        sensorSummary.Values.Add(new ValueSummaryModel())

                //update value summary
                let valueSummary = sensorSummary.Values.Item update.Index            
                match update.Update with
                | Min value -> valueSummary.Min <- value
                | Max value -> valueSummary.Max <- value
            
                index |> Map.add update.Device (deviceSummary, sensorIndex)            

        let readings = summarizeReadings timeout

        readings
        |> Observable.observeOn (DispatcherScheduler.Current)
        |> Observable.scanInit processUpdate index
        |> Observable.subscribe ignore

open System
open System.Windows
open SummaryTranslation

type SummaryViewModel() as this =
    inherit ViewModelBase()

    let mutable subscription : IDisposable = null
    let devices = new ObservableCollection<DeviceSummaryModel>()

    let updateSubscription timeoutDuration =
        if subscription <> null then do subscription.Dispose()
        let timeoutTimeSpan = TimeSpan.FromMilliseconds (float timeoutDuration)
        subscription <- subscribeToSummaryUpdates timeoutTimeSpan this.Devices

    let timeoutDuration = ref 300L

    let onClose cancelEventArgs =
        subscription.Dispose()
        Async.CancelDefaultToken()
        
    do 
        let mainWindow = Application.Current.MainWindow
        if mainWindow <> null then do
            mainWindow.Closing
            |> Event.add onClose

        updateSubscription !timeoutDuration

    with        
        member this.TimeoutDuration 
            with get() = !timeoutDuration
            and set value = setPropertyWithAction this <@@ this.TimeoutDuration @@> updateSubscription timeoutDuration value

        member this.Devices = devices
                