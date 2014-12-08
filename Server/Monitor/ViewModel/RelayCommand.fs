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

type ObservableCommand<'T>(onExecute : IObserver<'T>, converter) =    
    let event = new DelegateEvent<EventHandler>()        
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = true
        member x.Execute arg = 
            if onExecute <> null then do
                onExecute.OnNext (converter arg)