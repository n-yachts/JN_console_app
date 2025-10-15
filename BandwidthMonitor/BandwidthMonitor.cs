using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

class BandwidthMonitor
{
    static async Task Main()
    {
        Console.WriteLine("Síťový Bandwidth Monitor (stiskni Enter pro zastavení)\n");

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        NetworkInterface selectedInterface = null;

        foreach (NetworkInterface ni in interfaces)
        {
            if (ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                selectedInterface = ni;
                break;
            }
        }

        if (selectedInterface == null)
        {
            Console.WriteLine("Nenalezeno aktivní síťové rozhraní");
            return;
        }

        Console.WriteLine($"Sledování rozhraní: {selectedInterface.Name}\n");

        IPv4InterfaceStatistics initialStats = selectedInterface.GetIPv4Statistics();
        long initialBytesReceived = initialStats.BytesReceived;
        long initialBytesSent = initialStats.BytesSent;

        var cancellationTokenSource = new CancellationTokenSource();

        // Spustíme monitorování na pozadí
        var monitoringTask = Task.Run(() =>
            MonitorBandwidth(selectedInterface, initialBytesReceived, initialBytesSent, cancellationTokenSource.Token));

        // Čekáme na stisk Enter pro ukončení
        Console.ReadLine();
        cancellationTokenSource.Cancel();

        await monitoringTask;
    }

    static async Task MonitorBandwidth(NetworkInterface ni, long initialReceived, long initialSent, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastBytesReceived = initialReceived;
        long lastBytesSent = initialSent;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);

            IPv4InterfaceStatistics currentStats = ni.GetIPv4Statistics();
            long currentBytesReceived = currentStats.BytesReceived;
            long currentBytesSent = currentStats.BytesSent;

            long receivedDelta = currentBytesReceived - lastBytesReceived;
            long sentDelta = currentBytesSent - lastBytesSent;

            double receivedSpeed = receivedDelta * 8 / 1000.0; // kbps
            double sentSpeed = sentDelta * 8 / 1000.0; // kbps

            Console.WriteLine($"Stažení: {receivedSpeed:0.00} kbps | Odesláno: {sentSpeed:0.00} kbps | Celkem RX: {FormatBytes(currentBytesReceived)} | TX: {FormatBytes(currentBytesSent)}");

            lastBytesReceived = currentBytesReceived;
            lastBytesSent = currentBytesSent;
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
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