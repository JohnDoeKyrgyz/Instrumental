namespace Instrumental.ViewModel

open System.Reactive
open System
open System.Windows.Input

type ObservableCommand<'T>(onExecute : IObserver<'T>, converter) =    
    let event = new DelegateEvent<EventHandler>()        
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = true
        member x.Execute arg = 
            if onExecute <> null then do
                onExecute.OnNext (converter arg)