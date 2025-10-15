using System;
using System.Net;
using System.Threading.Tasks;

class SimpleDNSServer
{
    static async Task Main()
    {
        // Poznámka: Toto je velmi zjednodušená verze
        Console.WriteLine("Jednoduchý DNS Server (demonstrační)");

        // Simulace DNS dotazu
        string[] testDomains = { "google.com", "seznam.cz", "github.com" };

        foreach (string domain in testDomains)
        {
            try
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);
                Console.WriteLine($"\n{domain}:");
                foreach (IPAddress addr in addresses)
                {
                    Console.WriteLine($"  {addr} ({addr.AddressFamily})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pro {domain}: {ex.Message}");
            }
        }
    }
}