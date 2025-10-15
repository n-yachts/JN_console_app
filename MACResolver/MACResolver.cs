using System;
using System.Net;
using System.Net.NetworkInformation;

class MACResolver
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: MACResolver <IP/hostname>");
            return;
        }

        string target = args[0];

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(target);
            if (addresses.Length == 0)
            {
                Console.WriteLine("Nenalezena IP adresa pro zadaný hostname");
                return;
            }

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    PhysicalAddress mac = GetMACAddress(address);
                    if (mac != null)
                    {
                        Console.WriteLine($"IP: {address}");
                        Console.WriteLine($"MAC: {FormatMAC(mac)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static PhysicalAddress GetMACAddress(IPAddress ip)
    {
        // Toto je zjednodušená verze - v reálném prostředí by bylo potřeba
        // použít ARP tabulku nebo jiné metody
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses)
            {
                if (ipInfo.Address.Equals(ip))
                {
                    return ni.GetPhysicalAddress();
                }
            }
        }
        return null;
    }

    static string FormatMAC(PhysicalAddress mac)
    {
        string macString = mac.ToString();
        return string.Join(":",
            macString.Substring(0, 2),
            macString.Substring(2, 2),
            macString.Substring(4, 2),
            macString.Substring(6, 2),
            macString.Substring(8, 2),
            macString.Substring(10, 2));
    }
}