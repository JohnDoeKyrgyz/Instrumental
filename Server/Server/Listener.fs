namespace Instrumental

module Listener =

    open System.Net
    open System.Net.Sockets
    open System.Threading.Tasks
    open System

    open FSharp.Control.Reactive

    /// connect to the network
    let client =
        let client = new UdpClient(ExclusiveAddressUse = false)
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        let localEndpoint = IPEndPoint(IPAddress.Any, 5000)
        client.Client.Bind(localEndpoint)
        client

    /// An observable of datagrams from the local network
    let udpMessages =
        let signal = new Event<_>()
        let builder = async {
            while (true) do
                let! data = client.ReceiveAsync() |> Async.AwaitTask
                signal.Trigger data }

        //Async.StartAsTask(builder, TaskCreationOptions.LongRunning) |> ignore
        Async.StartAsTask(builder) |> ignore

        signal.Publish :> IObservable<_>

    /// Groups raw readings the address of the sender
    let readingsBySource =

        let readAddress (dataGram : UdpReceiveResult) = dataGram.RemoteEndPoint.Address.ToString()
        let readData (dataGram : UdpReceiveResult) = dataGram.Buffer

        udpMessages
        |> Observable.groupBy readAddress
        |> Observable.map (fun group -> group.Key, group |> Observable.map readData)

    /// Locks on to the stream of data from the first sender
    let singleSource =
        readingsBySource
        |> Observable.first
        |> Observable.flatmap snd