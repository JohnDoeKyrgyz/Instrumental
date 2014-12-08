namespace Instrumental.ViewModel

open System.Windows.Threading
open System.Collections.ObjectModel

open Instrumental.Summary

type ValueSummaryModel() =
    inherit ViewModelBase()
    let min = ref ValueSummary.Default
    let max = ref ValueSummary.Default        
    with                
        member this.Min
            with get() = !min
            and set value = setProperty this <@@ this.Min @@> min value        
        member this.Max
            with get() = !max
            and set value = setProperty this <@@ this.Max @@> max value

type SensorSummaryModel(device : string, sensor : int, resetSignal) =
    inherit ViewModelBase()
    let values = new ObservableCollection<ValueSummaryModel>([for i in 0 .. 3 -> new ValueSummaryModel()])
    let convert _ = ResetSensor (device, sensor)
    let resetCommand = new ObservableCommand<ResetMessage>(resetSignal, convert)
    with
        member this.Sensor = sensor
        member this.Values = values
        member this.Reset = resetCommand

type DeviceSummaryModel(name : string, resetSignal) =
    inherit ViewModelBase()
    let sensors = new ObservableCollection<SensorSummaryModel>()
    let convert (arg : obj) = ResetAll (arg :?> string)
    let resetCommand = new ObservableCommand<ResetMessage>(resetSignal, convert)
    with
        member this.Name = name
        member this.Sensors = sensors
        member this.Reset = resetCommand


module SummaryTranslation =

    open System
    open System.Collections.Generic
    open System.Linq
    open System.Reactive.Linq
    open System.Reactive.Concurrency
    open FSharp.Control.Reactive
    open System.Reactive.Subjects
    open System.Reactive.Disposables

    let subscribeToSummaryUpdates timeout (devices : ObservableCollection<DeviceSummaryModel>) =

        let resetSignal = new Subject<ResetMessage>()

        let find (collection : IEnumerable<'T>) (predicate : 'T -> bool) =
            match box (collection.SingleOrDefault( new Func<'T, bool>(predicate))) with
            | null -> None
            | value -> Some (value :?> 'T)

        let findDevice device = find devices (fun v -> v.Name = device)
        let findSensor (device : DeviceSummaryModel) sensor = find device.Sensors (fun v -> v.Sensor = sensor)

        let processUpdate updateMessage =
            
            match updateMessage with
            | Disconnect device ->
                let existingModel = findDevice device
                if existingModel.IsSome then do
                    devices.Remove existingModel.Value |> ignore

            | Connect device ->
                let existingModel = findDevice device
                if existingModel.IsNone then do
                    devices.Add (new DeviceSummaryModel(device, resetSignal) )

            | Update update ->

                let deviceSummary =
                    match findDevice update.Device with
                    | Some value -> value
                    | None -> failwith (sprintf "Device %s does not exist" update.Device)                

                //find or add sensor
                let sensorSummary =
                    match findSensor deviceSummary update.Sensor with
                    | Some value -> value
                    | None ->
                        let sensorSummary = new SensorSummaryModel(update.Device, update.Sensor, resetSignal)
                        deviceSummary.Sensors.Add(sensorSummary)
                        sensorSummary

                //update value summary
                let valueSummary = sensorSummary.Values.Item update.Index
                match update.Update with
                | Min value -> valueSummary.Min <- value
                | Max value -> valueSummary.Max <- value
                
                sensorSummary.Values.RemoveAt update.Index
                sensorSummary.Values.Insert( update.Index, valueSummary )
                
            | Reset reset -> 

                let doWithDevice device action =
                    let findDeviceResult = findDevice device
                    if findDeviceResult.IsSome then do
                        action findDeviceResult.Value

                let resetSensorSummary (sensorSummary : SensorSummaryModel) =
                    for value in sensorSummary.Values do
                        value.Max <- ValueSummary.Default
                        value.Min <- ValueSummary.Default

                match reset with
                | ResetAll device -> doWithDevice device (fun deviceSummary -> deviceSummary.Sensors |> Seq.iter resetSensorSummary )
                | ResetSensor (device, sensor) -> doWithDevice device (fun deviceSummary ->
                    let findSensorResult = findSensor deviceSummary sensor
                    if findSensorResult.IsSome then do
                        resetSensorSummary findSensorResult.Value )

        let readings = summarizeReadings timeout (resetSignal :> IObservable<ResetMessage>)

        let subscription =
            readings
            |> Observable.observeOn (DispatcherScheduler.Current)
            |> Observable.subscribe processUpdate

        Disposable.Create (fun () -> 
            subscription.Dispose()
            resetSignal.Dispose())        

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

    let timeoutDuration = ref 3000L

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
                