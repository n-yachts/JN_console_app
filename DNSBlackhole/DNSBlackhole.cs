using System;
using System.Net;
using System.Threading.Tasks;

class DNSBlackhole
{
    static async Task Main()
    {
        // Jednoduchý DNS server který blokuje škodlivé domény
        var listener = new System.Net.Sockets.UdpClient(53);

        Console.WriteLine("DNS Blackhole běží na portu 53...");

        // Seznam blokovaných domén
        string[] blockedDomains = { "malware.com", "ads.example.com", "tracker.com" };

        while (true)
        {
            var result = await listener.ReceiveAsync();
            byte[] data = result.Buffer;
            string domain = "unknown";

            try
            {
                // Jednoduchá extrakce doménového jména (zjednodušené)
                if (data.Length > 12)
                {
                    domain = System.Text.Encoding.UTF8.GetString(data, 12, data.Length - 12)
                        .Split('\0')[0];
                }

                Console.WriteLine($"Dotaz: {domain}");

                bool isBlocked = false;
                foreach (string blocked in blockedDomains)
                {
                    if (domain.Contains(blocked))
                    {
                        isBlocked = true;
                        break;
                    }
                }

                byte[] response;
                if (isBlocked)
                {
                    Console.WriteLine($"🚫 Blokováno: {domain}");
                    // Vrátíme 0.0.0.0 pro blokované domény
                    response = CreateBlockedResponse(data);
                }
                else
                {
                    // Normální DNS resolution
                    response = await CreateNormalResponse(data, domain);
                }

                await listener.SendAsync(response, response.Length, result.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }

    static byte[] CreateBlockedResponse(byte[] query)
    {
        // Zjednodušená implementace - v reálném světě by bylo potřeba
        // správně parsovat DNS packet a vytvořit validní response
        return query; // Placeholder
    }

    static async Task<byte[]> CreateNormalResponse(byte[] query, string domain)
    {
        // Přeposlání dotazu na skutečný DNS server
        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);
            // Zde by byla konstrukce DNS response packetu
            return query; // Placeholder
        }
        catch
        {
            return query;
        }
    }
}