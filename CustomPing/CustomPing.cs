using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class CustomPing
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: CustomPing <hostname/IP>");
            return;
        }

        string target = args[0];
        Ping ping = new Ping();

        for (int i = 0; i < 4; i++)
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(target, 1000);
                Console.WriteLine($"Odpověď od {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options.Ttl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
            await Task.Delay(1000);
        }
    }
}