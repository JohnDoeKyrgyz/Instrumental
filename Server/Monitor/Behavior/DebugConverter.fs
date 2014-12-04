namespace Instrumental.Behavior

open System
open System.Diagnostics
open System.Windows
open ConverterBase

type DebugConverter() =
    inherit ConverterBase()
    let convertFunc value _ _ _ =
        Debug.WriteLine( sprintf "Convert %A" value )
        value
    override this.Convert = convertFunc