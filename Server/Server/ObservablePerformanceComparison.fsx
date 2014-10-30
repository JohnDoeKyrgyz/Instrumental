/// This script compares two different styles of asynchronous observable to determine which one performs better.

#load "Instrumental.fsx"

open System
open System.Linq
open System.Reactive.Linq

open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders

open Instrumental.Listener
open System.Net
open System.Net.Sockets
open System.Reactive.Concurrency

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

// manual sequence scheduled on its own thread
let newThreadObservedSequence =
    let messages = seq {
        while(true) do
            let ipEndPoint = ref (new IPEndPoint(0L, 0))
            let data = client.Receive(ipEndPoint)
            let result = new UdpReceiveResult(data, !ipEndPoint)
            yield result }
    messages.ToObservable( NewThreadScheduler.Default )

type GameEntry = {
    Name: string
    Wins: int
}

type Game = {
    Play: int list
    Entrants: GameEntry list
    Round: int }
    with
        member this.PickWinner play =
            let winningValue = List.max play
            let winningIndex = play |> List.findIndex ((=) winningValue)
            List.nth this.Entrants winningIndex

        override this.ToString() =
        
            let stringify values = 
                values 
                |> Seq.map string 
                |> String.concat ", "

            let playWinner = (this.PickWinner this.Play).Name
            let cummulativeWins = [for entrant in this.Entrants -> entrant.Wins]
            let cummulativeWinner = (this.PickWinner cummulativeWins).Name

            let cummulativeWinsString = stringify cummulativeWins
            let playString = stringify this.Play

            sprintf "%d (%s) %s - (%s) %s" (this.Round) playString playWinner cummulativeWinsString cummulativeWinner

let continueGame (game : Game) play =
    let entrants =
        let winner = game.PickWinner play
        [for entrant in game.Entrants -> {entrant with Wins = entrant.Wins + if winner = entrant then  + 1 else 0}]
    { Play = play; Entrants = entrants; Round = game.Round + 1 }

let compare() =
    let throughput signal =
        signal
        |> Observable.bufferSpan (TimeSpan.FromMilliseconds 500.0)
        |> Observable.map (fun values -> values.Count)

    let names = ["Manual"; "Delayed"; "Thread"]
    let signals = [manualSequence; delayedSequence; newThreadObservedSequence]
    let initialEntrants = names |> List.map (fun name -> {Name = name; Wins = 0})
    let initialGame = {Play = []; Round = 0; Entrants = initialEntrants}

    signals
    |> List.map throughput
    |> Observable.zipSeq
    |> Observable.filter (fun play -> play.All( fun value -> value <> 0) )
    |> Observable.map Seq.toList
    |> Observable.scanInit continueGame initialGame
    |> Observable.subscribe (printfn "%O")

let running = compare()