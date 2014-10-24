namespace Instrumental

module Main =

    open System

    open Instrumental.Summary

    open FSharp.Control.Reactive

    [<EntryPoint>]
    let main argv = 
        
        printfn "Press any key to exit ..."

        let printScreen (screen : string) =
            Console.Clear()
            Console.SetCursorPosition(0, 0)
            Console.Out.WriteLine(screen)

        let onError (error : exn) = 
            printfn "ERROR %s" error.Message
            Console.Beep()

        let summary = 
            readingsSummary()
            |> Observable.scanInit (fun rows summary -> rows |> Map.add (summary.Name) summary) Map.empty            
            |> Observable.map (fun rows -> rows |> Map.toSeq |> Seq.map (snd >> string) |> String.concat Environment.NewLine)
            |> Observable.scanInit (fun (_, prev) screen -> prev = screen, screen) (false, String.Empty)
            |> Observable.filter fst
            |> Observable.map snd
            |> Observable.subscribeWithError printScreen onError

        Console.ReadKey() |> ignore
        summary.Dispose()
        
        printfn "Done"
        Console.Clear()

        0 // return an integer exit code
