using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TrafficGenerator
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Použití: TrafficGenerator <target> <port> <packets>");
            return;
        }

        string target = args[0];
        int port = int.Parse(args[1]);
        int packets = int.Parse(args[2]);

        using (UdpClient client = new UdpClient())
        {
            byte[] data = Encoding.UTF8.GetBytes("Test packet");

            for (int i = 0; i < packets; i++)
            {
                await client.SendAsync(data, data.Length, target, port);
                Console.WriteLine($"Odeslán packet {i + 1}/{packets}");
                await Task.Delay(100);
            }
        }
    }
}