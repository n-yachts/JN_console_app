using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class PortScanner
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: PortScanner <hostname/IP>");
            return;
        }

        string target = args[0];
        int[] commonPorts = { 21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995 };

        foreach (int port in commonPorts)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(target, port);
                    Console.WriteLine($"Port {port}: OTEVŘENÝ");
                }
                catch
                {
                    Console.WriteLine($"Port {port}: ZAVŘENÝ");
                }
            }
        }
    }
}