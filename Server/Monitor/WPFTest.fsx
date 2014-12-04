#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml"

#r @"..\packages\Rx-Core.2.2.5\lib\net40\System.Reactive.Core.dll"
#r @"..\packages\Rx-Linq.2.2.5\lib\net40\System.Reactive.Linq.dll"
#r @"..\packages\Rx-Interfaces.2.2.5\lib\net40\System.Reactive.Interfaces.dll"
#r @"..\packages\Rx-PlatformServices.2.2.5\lib\net45\System.Reactive.PlatformServices.dll"
#r @"..\packages\Rx-XAML.2.2.5\lib\net45\System.Reactive.Windows.Threading.dll"

#r @"bin\Debug\InstrumentalServer.dll"

open System
open System.Threading
open System.Windows
open System.Windows.Threading
open System.Windows.Controls
open System.ComponentModel
open System.Reactive.Concurrency

open Instrumental
open Instrumental.Listener

open FSharp.Control.Reactive
open System.Windows.Data

type Reading = {
    Value : int64 }
    with
        override this.ToString() = sprintf "%d" this.Value

type Model() =
    let refValue = ref {Value = 0L}
    let mutable mutableValue = {Value = 0L}
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
    member x.OnPropertyChanged (propertyName) = 
        propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])
    member this.Ref
        with get() = !refValue
        and set value =
            refValue := value
            this.OnPropertyChanged("Ref")
    member this.Mutable
        with get() = mutableValue
        and set value = 
            mutableValue <- value
            this.OnPropertyChanged("Mutable")

let panel = new StackPanel()

let addTextBox (path : string) =
    let textBox = new TextBox()
//    let valueConverter = {
//        new IValueConverter with
//            member x.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj = 
//                printfn "Convert %A" value
//                box (string value)
//            member x.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj = 
//                printfn "ConvertBack %A" value
//                value }
    let binding = new Binding(path + ".Value", Mode = BindingMode.OneWay)    
    let bindingExpression = textBox.SetBinding(TextBox.TextProperty, binding)
    
    panel.Children.Add(textBox) |> ignore

addTextBox "Ref"
addTextBox "Mutable"

let w = Window(Title = "Test")
w.Content <- panel

let model = new Model()
panel.DataContext <- model

Observable.interval (TimeSpan.FromMilliseconds 500.0)
|> Observable.map (fun v -> { Value = v })
|> Observable.subscribe (fun value -> model.Mutable <- value; model.Ref <- value )

(*
Summary.summarizeReadings 100
|> Observable.observeOn (DispatcherScheduler.Current)
|> Observable.subscribe (fun v -> w.Title <- string v)
*)

let application = new Application()
application.Run(w)