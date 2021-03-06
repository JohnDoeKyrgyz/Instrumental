﻿namespace Instrumental.Behavior

open System
open System.Windows

/// Returns Visibility.Visible if the string is not null or empty
type StringExistsToVisibilityConverter() =
    inherit ConverterBase()
    let convertFunc = fun (v:obj) _ _ _ ->         
        match String.IsNullOrEmpty(string v) with
        | false -> Visibility.Visible
        | _ -> Visibility.Collapsed
        :> obj
    override this.Convert = convertFunc 

