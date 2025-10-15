using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ThroughputTester
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Použití: ThroughputTester <server> <port> <velikost_MB>");
            return;
        }

        string server = args[0];
        int port = int.Parse(args[1]);
        int sizeMB = int.Parse(args[2]);

        byte[] testData = new byte[sizeMB * 1024 * 1024];
        new Random().NextBytes(testData);

        using TcpClient client = new TcpClient();
        Stopwatch stopwatch = Stopwatch.StartNew();

        await client.ConnectAsync(server, port);
        NetworkStream stream = client.GetStream();

        await stream.WriteAsync(testData, 0, testData.Length);
        stopwatch.Stop();

        double speed = (testData.Length * 8) / (stopwatch.Elapsed.TotalSeconds * 1000000);
        Console.WriteLine($"Odesláno {sizeMB} MB za {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Průtok: {speed:F2} Mbps");
    }
}