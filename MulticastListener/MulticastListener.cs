using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class MulticastListener
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: MulticastListener <multicast_group> <port>");
            Console.WriteLine("Příklad: MulticastListener 224.0.0.1 5000");
            return;
        }

        IPAddress multicastGroup = IPAddress.Parse(args[0]);
        int port = int.Parse(args[1]);

        UdpClient client = new UdpClient();
        client.JoinMulticastGroup(multicastGroup);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        Console.WriteLine($"Naslouchám multicast skupině {multicastGroup}:{port}");

        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            byte[] data = client.Receive(ref remote);
            string message = Encoding.UTF8.GetString(data);
            Console.WriteLine($"[{remote}] {message}");
        }
    }
}