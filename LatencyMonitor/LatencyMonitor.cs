using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class LatencyMonitor
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Použití: LatencyMonitor <host1> <host2> ...");
            return;
        }

        Console.WriteLine("Monitoring latence (Ctrl+C pro ukončení)\n");

        while (true)
        {
            foreach (string host in args)
            {
                try
                {
                    long latency = await MeasureLatency(host);
                    Console.WriteLine($"{DateTime.Now:T} {host}: {latency}ms");
                }
                catch
                {
                    Console.WriteLine($"{DateTime.Now:T} {host}: TIMEOUT");
                }
            }

            Console.WriteLine("---");
            await Task.Delay(5000);
        }
    }

    static async Task<long> MeasureLatency(string host)
    {
        Ping ping = new Ping();
        Stopwatch sw = Stopwatch.StartNew();

        PingReply reply = await ping.SendPingAsync(host, 1000);
        sw.Stop();

        if (reply.Status != IPStatus.Success)
            throw new Exception("Ping failed");

        return reply.RoundtripTime;
    }
}