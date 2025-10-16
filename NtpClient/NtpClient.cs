using System;
using System.Net;
using System.Net.Sockets;

class NtpClient
{
    static void Main(string[] args)
    {
        string[] ntpServers = {
            "pool.ntp.org",
            "time.google.com",
            "time.windows.com",
            "time.nist.gov"
        };

        Console.WriteLine("NTP Time Synchronizer\n");

        foreach (string server in ntpServers)
        {
            try
            {
                DateTime ntpTime = GetNetworkTime(server);
                DateTime localTime = DateTime.Now;
                TimeSpan difference = ntpTime - localTime;

                Console.WriteLine($"Server: {server}");
                Console.WriteLine($"NTP Time:    {ntpTime:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Local Time:  {localTime:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Difference:  {difference.TotalMilliseconds:+0.##;-0.##} ms");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba u {server}: {ex.Message}\n");
            }
        }
    }

    static DateTime GetNetworkTime(string ntpServer)
    {
        var ntpData = new byte[48];
        ntpData[0] = 0x1B; // LI = 0, VN = 3, Mode = 3 (Client)

        var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        var ipEndPoint = new IPEndPoint(addresses[0], 123);

        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
        }

        const byte serverReplyTime = 40;
        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        intPart = SwapEndianness(intPart);
        fractPart = SwapEndianness(fractPart);

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMilliseconds((long)milliseconds);

        return networkDateTime.ToLocalTime();
    }

    static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }
}