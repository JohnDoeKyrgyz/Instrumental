namespace Instrumental

module Listener =

    open System
    open System.Net
    open System.Net.Sockets
    open System.Threading
    open System.Threading.Tasks
    open System.Reactive.Subjects
    open System.Reactive.Concurrency

    open FSharp.Control.Reactive

    /// connect to the network
    let client =
        let client = new UdpClient(ExclusiveAddressUse = false)
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
        let localEndpoint = IPEndPoint(IPAddress.Any, 5000)
        client.Client.Bind(localEndpoint)
        client

    /// An infinite sequence of messages from the network
    let messages= seq {
        let cancellationToken = Async.DefaultCancellationToken
        while(not cancellationToken.IsCancellationRequested) do
            let ipEndPoint = ref (new IPEndPoint(0L, 0))
            let data = client.Receive(ipEndPoint)
            let result = new UdpReceiveResult(data, !ipEndPoint)
            yield result }

    /// An observable of datagrams from the local network
    let udpMessages =        
        let backgroundThreadScheduler = new NewThreadScheduler( fun threadStart -> new Thread(threadStart, IsBackground = true))
        messages
        |> Observable.ofSeqOn backgroundThreadScheduler

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