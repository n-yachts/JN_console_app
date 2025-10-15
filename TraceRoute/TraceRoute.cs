using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class TraceRoute
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: TraceRoute <hostname/IP>");
            return;
        }

        string target = args[0];
        int maxHops = 30;
        int timeout = 1000;

        Console.WriteLine($"Trasování k {target} s maximálně {maxHops} skoky:\n");

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            using (Ping ping = new Ping())
            {
                PingOptions options = new PingOptions(ttl, true);
                byte[] buffer = new byte[32];
                PingReply reply = await ping.SendPingAsync(target, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms");
                    Console.WriteLine("Trasování dokončeno.");
                    break;
                }
                else if (reply.Status == IPStatus.TtlExpired)
                {
                    Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms");
                }
                else
                {
                    Console.WriteLine($"{ttl}\t*");
                }

                if (ttl == maxHops)
                {
                    Console.WriteLine("Dosaženo maximálního počtu skoků.");
                }
            }
        }
    }
}