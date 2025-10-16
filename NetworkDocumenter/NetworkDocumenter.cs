using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

class NetworkDocumenter
{
    static void Main()
    {
        Console.WriteLine("=== NETWORK DOCUMENTATION REPORT ===\n");

        DocumentNetworkInterfaces();
        DocumentRoutingTable();
        DocumentDNSServers();
        DocumentNetworkStatistics();
    }

    static void DocumentNetworkInterfaces()
    {
        Console.WriteLine("1. SÍŤOVÁ ROZHRANÍ:");
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            Console.WriteLine($"\n{ni.Name} ({ni.NetworkInterfaceType})");
            Console.WriteLine($"  Status: {ni.OperationalStatus}");
            Console.WriteLine($"  Popis: {ni.Description}");
            Console.WriteLine($"  MAC: {ni.GetPhysicalAddress()}");
            Console.WriteLine($"  Rychlost: {ni.Speed / 1000000} Mbps");

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Console.WriteLine($"  IPv4: {ip.Address}/{ip.IPv4Mask}");
                }
            }
        }
        Console.WriteLine();
    }

    static void DocumentRoutingTable()
    {
        Console.WriteLine("2. SMĚROVACÍ TABULKA:");
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        var gateways = properties.GetIPGlobalProperties().GetActiveTcpConnections();

        // Zjednodušené zobrazení výchozí brány
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in interfaces)
        {
            var gateway = ni.GetIPProperties().GatewayAddresses;
            if (gateway.Count > 0)
            {
                Console.WriteLine($"  {ni.Name}: {gateway[0].Address}");
            }
        }
        Console.WriteLine();
    }

    static void DocumentDNSServers()
    {
        Console.WriteLine("3. DNS SERVERY:");
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            var dnsServers = ni.GetIPProperties().DnsAddresses;
            if (dnsServers.Count > 0)
            {
                Console.WriteLine($"  {ni.Name}:");
                foreach (IPAddress dns in dnsServers)
                {
                    Console.WriteLine($"    {dns}");
                }
            }
        }
        Console.WriteLine();
    }

    static void DocumentNetworkStatistics()
    {
        Console.WriteLine("4. STATISTIKY:");
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

        Console.WriteLine($"  Aktivní TCP spojení: {properties.GetActiveTcpConnections().Length}");
        Console.WriteLine($"  Aktivní TCP listenery: {properties.GetActiveTcpListeners().Length}");
        Console.WriteLine($"  Aktivní UDP listenery: {properties.GetActiveUdpListeners().Length}");
    }
}