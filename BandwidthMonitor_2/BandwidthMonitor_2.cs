using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

class BandwidthMonitor_2
{
    static void Main()
    {
        Console.WriteLine("Monitorování šířky pásma (Ctrl+C pro ukončení)\n");

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in interfaces)
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                Console.WriteLine($"Monitorování rozhraní: {ni.Name}");
                _ = Task.Run(() => MonitorInterface(ni));
            }
        }

        Console.ReadLine();
    }

    static void MonitorInterface(NetworkInterface ni)
    {
        long lastBytesSent = ni.GetIPv4Statistics().BytesSent;
        long lastBytesReceived = ni.GetIPv4Statistics().BytesReceived;

        while (true)
        {
            Thread.Sleep(1000);
            var stats = ni.GetIPv4Statistics();
            long currentBytesSent = stats.BytesSent;
            long currentBytesReceived = stats.BytesReceived;

            long sentSpeed = currentBytesSent - lastBytesSent;
            long receivedSpeed = currentBytesReceived - lastBytesReceived;

            Console.WriteLine($"{ni.Name}: Upload: {FormatBytes(sentSpeed)}/s, Download: {FormatBytes(receivedSpeed)}/s");

            lastBytesSent = currentBytesSent;
            lastBytesReceived = currentBytesReceived;
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