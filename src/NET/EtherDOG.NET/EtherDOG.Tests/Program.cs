using EtherDOGNET;

public class Program
{
    public static void Main(string[] args)
    {
        var eth = new EtherDOG("localhost", 50000);

        eth.OnStatusChanged += OnStatusChanged;
        eth.OnReceivedData += OnReceivedData;
        eth.OnSendData += OnSendData;

        eth.Start();

        Console.ReadKey();

        eth.Stop();

        Console.ReadKey();
    }

    static void OnStatusChanged(bool connected)
    {
        if (connected)
        {
            Console.WriteLine("A client got connected.");
        }
        else
        {
            Console.WriteLine("A client got disconnected.");
        }
    }

    static void OnReceivedData(byte[] data)
    {
        // Here we can parse the data
        var ios = data.Take(8).ToArray();

        var bit0 = ReadBit(ios[0], 0);
        var bit1 = ReadBit(ios[0], 1);
        var bit2 = ReadBit(ios[0], 2);
        var bit3 = ReadBit(ios[0], 3);
        var bit4 = ReadBit(ios[0], 4);
        var bit5 = ReadBit(ios[0], 5);
        var bit6 = ReadBit(ios[0], 6);
        var bit7 = ReadBit(ios[0], 7);
    }

    static byte[] OnSendData()
    {
        var data = new List<byte>();

        var iOs = new byte[8];

        data.AddRange(iOs);

        // Here we can send the data

        return data.ToArray();
    }

    static bool ReadBit(byte b, int index)
    {
        return (b & (1 << index)) != 0;
    }

    static void SetBit(ref byte b, int index, bool set)
    {
        byte mask = (byte)(1 << index);

        if (set)
            b = (byte)(b | mask);
        else
            b = (byte)(b & ~mask);
    }
}
