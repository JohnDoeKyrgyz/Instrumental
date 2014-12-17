namespace Instrumental.ViewModel

open System
open System.Windows.Input


type RelayCommand (canExecute:(obj -> bool), action:(obj -> unit)) =
    let event = new DelegateEvent<EventHandler>()
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = canExecute(arg)
        member x.Execute arg = action(arg)