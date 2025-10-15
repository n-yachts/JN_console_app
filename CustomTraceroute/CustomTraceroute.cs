using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class CustomTraceroute
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: CustomTraceroute <cíl>");
            return;
        }

        string target = args[0];
        int maxHops = 30;
        int timeout = 1000;

        using (Ping ping = new Ping())
        {
            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = await ping.SendPingAsync(target, timeout, new byte[32], options);

                Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms\t{reply.Status}");

                if (reply.Status == IPStatus.Success)
                    break;
            }
        }
    }
}