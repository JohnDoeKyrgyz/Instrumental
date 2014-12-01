namespace Instrumental
    module Protocol =
        open System

        ///Fully decoded data from a sensor reading.
        type Message = {
            Sensor : int
            Time : int64
            Values : float32[]}
            with
                override this.ToString() =
                    let formattedValues =
                        this.Values
                        |> Array.map (sprintf "%-12f")
                        |> String.concat " "
                    sprintf "%-2i %i %s" this.Sensor this.Time formattedValues

        let private readValue offset length converter (data : byte array) =
            let lastIndex = length - 1
            let valueBytes = [|for i in 0 .. lastIndex -> data.[offset + (lastIndex - i)]|]
            converter(valueBytes, 0)
            
        ///Reads the time section of a sensor message. The time is the android device's System.nanoTime().
        let readTime = readValue 0 8 BitConverter.ToInt64

        ///Reads the sensor number from the sensor message. Sensor numbers are defined in the android API.
        let readSensor = readValue 8 4 BitConverter.ToInt32
        
        ///Reads the array of values in the sensor reading. The number and meaning of values will be different for each sensor. See the android documentation.
        let readValues (data : byte array) =
            let word = 4
            let offset = 12            
            let valuesCount = (data.Length - offset) / word
            [|for i in 0 .. valuesCount - 1 -> readValue (offset + (i * word)) word BitConverter.ToSingle data|]