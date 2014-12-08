namespace Instrumental.ViewModel

open System
open System.Reactive.Disposables
open System.Windows.Input

open FSharp.Control.Reactive

type RelayCommand (canExecute:(obj -> bool), action:(obj -> unit)) =
    let event = new DelegateEvent<EventHandler>()
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = canExecute(arg)
        member x.Execute arg = action(arg)

type ObservableCommand(?canExecuteChanged : IObservable<bool>) as this =    
    let event = new DelegateEvent<EventHandler>()
    let canExecute = ref true
    let mutable onExecute : IObserver<obj> = null
    let signal = Observable.create (fun observer -> new Action(fun () -> onExecute <- observer) )
    
    let canExcuteChangedSubscription =
        match canExecuteChanged with
        | Some signal -> 
            let onCanExecuteChanged value =
                canExecute := value
                event.Trigger( Array.empty )                
            signal |> Observable.subscribe onCanExecuteChanged
        | None -> Disposable.Empty

    member this.Execute = signal
    
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = !canExecute
        member x.Execute arg = 
            if onExecute <> null then do
                onExecute.OnNext arg

    interface IDisposable with        
        member this.Dispose() =
            onExecute.OnCompleted()
            canExcuteChangedSubscription.Dispose()