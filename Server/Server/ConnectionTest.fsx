/// This script compares two different styles of asynchronous observable to determine which one performs better.

#load "Instrumental.fsx"

open System
open System.Reactive.Linq

open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders

open Instrumental.Listener

// manual sequence with a long running task
let manualSequence =
    let signal = new Event<_>()
    async {
        while (true) do
            let! data = client.ReceiveAsync() |> Async.AwaitTask
            signal.Trigger data }
    |> Async.StartAsTask
    |> ignore

    signal.Publish :> IObservable<_>

// delayed async sequence
let delayedSequence = observe { return! client.ReceiveAsync |> Observable.FromAsync |> Observable.repeat }

type Game = {
    Play: int * int
    ManualWins: int
    DelayedWins: int
    Round: int }
    with
        override this.ToString() =

            let winner manual delayed =
                if manual = delayed then "Draw"
                else if manual > delayed then "Manual"
                else "Delayed"

            let playWinner = winner (fst this.Play) (snd this.Play)
            let cummulativeWinner = winner (this.ManualWins) (this.DelayedWins)            
            sprintf "%d (%d, %d) %s - (%d, %d) %s" (this.Round) (fst this.Play) (snd this.Play) playWinner (this.ManualWins) (this.DelayedWins) cummulativeWinner

let continueGame game (manual, delayed) =
    let manualWins, delayedWins =
        if(manual = delayed) then game.ManualWins, game.DelayedWins
        else if(manual > delayed) then game.ManualWins + 1, game.DelayedWins
        else game.ManualWins, game.DelayedWins + 1
    { Play = (manual, delayed); ManualWins = manualWins; DelayedWins = delayedWins; Round = game.Round + 1 }

let compare() =
    let throughput signal =
        signal
        |> Observable.bufferSpan (TimeSpan.FromMilliseconds 500.0)
        |> Observable.map (fun values -> values.Count)

    let manual = throughput manualSequence
    let delayed = throughput delayedSequence

    manual
    |> Observable.zip (fun m d -> m, d) delayed
    |> Observable.filter (fun (a, b) -> a > 0 && b > 0)
    |> Microsoft.FSharp.Control.Observable.scan continueGame {Play = 0, 0; ManualWins = 0; DelayedWins = 0; Round = 0}
    |> Observable.subscribe (printfn "%O")

let running = compare()