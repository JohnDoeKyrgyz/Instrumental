namespace Instrumental

module Listener =

    open System.Net
    open System.Net.Sockets
    
    /// An infinite sequence of datagrams broadcast from devices on the local network.
    let messages = 
        let client = new UdpClient(ExclusiveAddressUse = false)
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)    
    
        let localEndpoint = IPEndPoint(IPAddress.Any, 5000)
        client.Client.Bind(localEndpoint)        
        seq {
            while (true) do
                let endPointRef = ref localEndpoint
                let data = client.Receive(endPointRef)
                yield endPointRef.Value, data }