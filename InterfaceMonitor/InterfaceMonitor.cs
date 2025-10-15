using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

class InterfaceMonitor
{
    static async Task Main()
    {
        Console.WriteLine("Monitor síťových rozhraní (Ctrl+C pro ukončení)\n");

        using var cancellationToken = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            cancellationToken.Cancel();
        };

        while (!cancellationToken.Token.IsCancellationRequested)
        {
            Console.Clear();
            DisplayInterfaceInfo();
            await Task.Delay(2000, cancellationToken.Token);
        }
    }

    static void DisplayInterfaceInfo()
    {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            Console.WriteLine($"{ni.Name} ({ni.NetworkInterfaceType})");
            Console.WriteLine($"  Status: {ni.OperationalStatus}");
            Console.WriteLine($"  Speed: {ni.Speed / 1000000} Mbps");

            var stats = ni.GetIPv4Statistics();
            Console.WriteLine($"  RX: {FormatBytes(stats.BytesReceived)} | TX: {FormatBytes(stats.BytesSent)}");
            Console.WriteLine();
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}