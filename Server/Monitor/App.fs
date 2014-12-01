module Instrumental.Monitor

open System
open System.Windows
open Instrumental.ViewModel

// Create the View and bind it to the View Model
let mainWindowViewModel = 
    let window = new System.Uri("/App;component/mainwindow.xaml", UriKind.Relative)
    Application.LoadComponent(window) :?> Window
                             
mainWindowViewModel.DataContext <- new MainWindowViewModel() 

// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main(_) = (new Application()).Run(mainWindowViewModel)