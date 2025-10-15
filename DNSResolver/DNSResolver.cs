using System;
using System.Net;

class DNSResolver
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: DNSResolver <hostname>");
            return;
        }

        string hostname = args[0];

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);
            Console.WriteLine($"DNS záznamy pro {hostname}:");

            foreach (IPAddress address in addresses)
            {
                Console.WriteLine($"  {address} ({address.AddressFamily})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}