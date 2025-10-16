using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class TopologyMapper
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: TopologyMapper <network/CIDR>");
            Console.WriteLine("Příklad: TopologyMapper 192.168.1.0/24");
            return;
        }

        string[] parts = args[0].Split('/');
        IPAddress network = IPAddress.Parse(parts[0]);
        int cidr = int.Parse(parts[1]);

        int hostBits = 32 - cidr;
        uint hostCount = (uint)Math.Pow(2, hostBits) - 2;

        Console.WriteLine($"Skenování sítě {network}/{cidr} ({hostCount} hostů)");

        byte[] ipBytes = network.GetAddressBytes();
        uint baseIp = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) |
                     ((uint)ipBytes[2] << 8) | ipBytes[3];

        var tasks = new Task[Math.Min(50, hostCount)]; // Omezení paralelismu

        for (uint i = 1; i <= hostCount; i++)
        {
            uint ip = baseIp + i;
            var task = CheckHost(ip);
            tasks[(i - 1) % tasks.Length] = task;

            if (i % tasks.Length == 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }

    static async Task CheckHost(uint ip)
    {
        IPAddress address = new IPAddress(ip);

        using (Ping ping = new Ping())
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(address, 1000);
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"🟢 {address} - {reply.RoundtripTime}ms");

                    // Pokus o získání hostname
                    try
                    {
                        IPHostEntry hostEntry = await Dns.GetHostEntryAsync(address);
                        Console.WriteLine($"   Hostname: {hostEntry.HostName}");
                    }
                    catch
                    {
                        Console.WriteLine($"   Hostname: nepodařilo se získat");
                    }
                }
            }
            catch
            {
                // Ignorovat chyby
            }
        }
    }
}