using System;  // Import základních systémových knihoven
using System.Collections.Generic;  // Import kolekcí (List)
using System.Linq;  // Import LINQ (IPv4 filtr)
using System.Net;  // Import knihoven pro síťovou komunikaci
using System.Net.Sockets;  // Import UDP klienta
using System.Text;  // Import kódování textu
using System.Threading.Tasks;  // Import knihoven pro asynchronní programování

class DNSBlackhole  // Hlavní třída DNS blackhole filtru
{
    static async Task Main()  // Hlavní asynchronní metoda
    {
        // Vytvoření UDP socketu naslouchajícího na standardním DNS portu 53
        var listener = new UdpClient(53);

        // Informační zpráva o spuštění služby
        Console.WriteLine("DNS Blackhole běží na portu 53...");

        // Seznam domén které budou blokovány (vrací se 0.0.0.0)
        string[] blockedDomains = { "malware.com", "ads.example.com", "tracker.com" };

        // Hlavní smyčka serveru - nekonečně čeká na příchozí požadavky
        while (true)
        {
            UdpReceiveResult result;
            try
            {
                // Asynchronní přijetí DNS dotazu
                result = await listener.ReceiveAsync();
            }
            catch (SocketException ex)
            {
                // Např. ICMP "port unreachable" po odeslání odpovědi klientovi, který už neposlouchá
                Console.WriteLine($"Chyba při příjmu: {ex.Message}");
                continue;
            }

            // Extrakce dat z UDP paketu
            byte[] data = result.Buffer;

            try
            {
                // Zpracování DNS dotazu: doménové jméno a typ dotazu
                if (!TryParseQuestion(data, out string domain, out ushort qtype, out int questionEnd))
                    continue;  // Nevalidní paket se ignoruje

                // Výpis přijatého dotazu do konzole
                Console.WriteLine($"Dotaz: {domain} (typ {qtype})");

                // Kontrola zda je doména v blacklistu (přesná shoda nebo subdoména)
                bool isBlocked = blockedDomains.Any(b =>
                    domain.Equals(b, StringComparison.OrdinalIgnoreCase) ||
                    domain.EndsWith("." + b, StringComparison.OrdinalIgnoreCase));

                byte[] response;  // Proměnná pro odpověď
                if (isBlocked)
                {
                    // Výpis blokované domény s indikátorem
                    Console.WriteLine($"🚫 Blokováno: {domain}");
                    // Vytvoření odpovědi s IP 0.0.0.0
                    response = CreateResponse(data, questionEnd, qtype, new[] { IPAddress.Any }, 0);
                }
                else
                {
                    // Normální překlad přes systémový DNS server
                    response = await CreateNormalResponse(data, questionEnd, qtype, domain);
                }

                // Odeslání odpovědi zpět klientovi
                await listener.SendAsync(response, response.Length, result.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                // Zachycení a výpis chyb při zpracování
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }

    // Parsování první otázky DNS dotazu (hlavička 12 B, poté QNAME jako labely s délkou, QTYPE, QCLASS)
    static bool TryParseQuestion(byte[] data, out string domain, out ushort qtype, out int questionEnd)
    {
        domain = null;
        qtype = 0;
        questionEnd = 0;

        // Hlavička + alespoň jeden label + QTYPE/QCLASS; dotaz musí mít QR=0 a QDCOUNT >= 1
        if (data.Length < 17 || (data[2] & 0x80) != 0 || ((data[4] << 8) | data[5]) < 1)
            return false;

        var labels = new List<string>();
        int pos = 12;

        while (true)
        {
            if (pos >= data.Length) return false;
            int len = data[pos++];
            if (len == 0) break;
            if ((len & 0xC0) != 0 || pos + len > data.Length) return false;  // Komprese se v dotazu neočekává
            labels.Add(Encoding.ASCII.GetString(data, pos, len));
            pos += len;
        }

        if (pos + 4 > data.Length) return false;

        qtype = (ushort)((data[pos] << 8) | data[pos + 1]);
        questionEnd = pos + 4;
        domain = string.Join(".", labels);
        return true;
    }

    // Sestavení DNS odpovědi: kopie hlavičky a otázky, QR=1, RA=1 a A záznamy (pouze pro typ A)
    static byte[] CreateResponse(byte[] query, int questionEnd, ushort qtype, IPAddress[] addresses, byte rcode)
    {
        var ipv4 = qtype == 1
            ? addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray()
            : new IPAddress[0];

        var response = new List<byte>(questionEnd + ipv4.Length * 16);
        response.AddRange(query.Take(questionEnd));  // Hlavička + otázka

        response[2] = (byte)(0x80 | (query[2] & 0x79));  // QR=1, zachován opcode a RD, AA=0, TC=0
        response[3] = (byte)(0x80 | rcode);               // RA=1 + kód odpovědi
        response[4] = 0; response[5] = 1;                 // QDCOUNT = 1
        response[6] = (byte)(ipv4.Length >> 8);           // ANCOUNT
        response[7] = (byte)ipv4.Length;
        response[8] = response[9] = response[10] = response[11] = 0;  // NSCOUNT, ARCOUNT

        foreach (IPAddress address in ipv4)
        {
            response.AddRange(new byte[] { 0xC0, 0x0C });        // Ukazatel na jméno v otázce
            response.AddRange(new byte[] { 0x00, 0x01 });        // Typ A
            response.AddRange(new byte[] { 0x00, 0x01 });        // Třída IN
            response.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x3C });  // TTL 60 s
            response.AddRange(new byte[] { 0x00, 0x04 });        // Délka RDATA
            response.AddRange(address.GetAddressBytes());        // IPv4 adresa
        }

        return response.ToArray();
    }

    // Normální DNS překlad přes systémový resolver
    static async Task<byte[]> CreateNormalResponse(byte[] query, int questionEnd, ushort qtype, string domain)
    {
        try
        {
            // Asynchronní dotaz na systémový DNS server
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);
            return CreateResponse(query, questionEnd, qtype, addresses, 0);
        }
        catch (SocketException)
        {
            // Doména neexistuje - NXDOMAIN (RCODE 3)
            return CreateResponse(query, questionEnd, qtype, new IPAddress[0], 3);
        }
    }
}

/*
Zjednodušený DNS blackhole filtr:
 Čte první otázku dotazu (QNAME je posloupnost labelů s délkou, ne řetězec ukončený nulou)
 Blokované domény (včetně subdomén) dostanou odpověď A 0.0.0.0
 Ostatní domény se přeloží systémovým resolverem (pouze A záznamy), neexistující vrací NXDOMAIN
Omezení: neřeší AAAA, CNAME, EDNS, TCP ani cache. Pro skutečné nasazení použijte např. Pi-hole.
*/
