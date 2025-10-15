using System;
using System.Net;
using System.Net.NetworkInformation;

class NetworkInfo
{
    static void Main()
    {
        // Lokální informace
        string hostName = Dns.GetHostName();
        Console.WriteLine($"Hostname: {hostName}");

        // Síťová rozhraní
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                Console.WriteLine($"\nRozhraní: {ni.Name}");
                Console.WriteLine($"  Typ: {ni.NetworkInterfaceType}");
                Console.WriteLine($"  Popis: {ni.Description}");

                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Console.WriteLine($"  IPv4: {ip.Address}");
                        Console.WriteLine($"  Maska: {ip.IPv4Mask}");
                    }
                }
            }
        }
    }
}