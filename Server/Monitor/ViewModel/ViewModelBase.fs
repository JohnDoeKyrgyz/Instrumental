namespace Instrumental.ViewModel

open System.ComponentModel

type ViewModelBase() =
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
    member x.OnPropertyChanged (propertyName) = 
        propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])

[<AutoOpen>]
module ViewModelHelper =
    
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.Patterns

    let setPropertyWithAction (viewModel : 'TViewModelBase when 'TViewModelBase :> ViewModelBase) propertyNameExpression propertySetAction (variable : ref<'T>) value =
        
        if value <> !variable then do
            variable := value

            let propertyName =
                match propertyNameExpression with
                | PropertyGet( _, propertyInfo, _) -> propertyInfo.Name
                | _ -> failwith "Expected a PropetyGet expression"

            propertySetAction value

            viewModel.OnPropertyChanged(propertyName)

    let setProperty (viewModel : 'TViewModelBase when 'TViewModelBase :> ViewModelBase) propertyNameExpression variable value =        
        setPropertyWithAction viewModel propertyNameExpression ignore variable value
