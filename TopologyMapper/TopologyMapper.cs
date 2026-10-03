using System;  // Základní jmenný prostor pro vstupy/výstupy, řetězce atd.
using System.Net;  // Práce s IP adresami, DNS a síťovými nástroji
using System.Net.NetworkInformation;  // Ping funkce
using System.Threading.Tasks;  // Asynchronní programování

class TopologyMapper  // Hlavní třída pro mapování sítě
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda
    {
        // Kontrola počtu argumentů
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: TopologyMapper <network/CIDR>");
            Console.WriteLine("Příklad: TopologyMapper 192.168.1.0/24");
            return;  // Ukončení programu při chybném vstupu
        }

        // Rozdělení vstupního argumentu (např. "192.168.1.0/24")
        string[] parts = args[0].Split('/');

        // Kontrola formátu vstupu (IP/CIDR)
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress network) ||
            network.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
            !int.TryParse(parts[1], out int cidr) || cidr < 1 || cidr > 30)
        {
            Console.WriteLine("Neplatný vstup. Použijte IPv4 adresu a prefix /1 až /30.");
            return;
        }

        // Výpočet počtu bitů pro hostitele
        int hostBits = 32 - cidr;

        // Výpočet počtu hostitelů v síti (odečítáme síť a broadcast)
        uint hostCount = (uint)((1UL << hostBits) - 2);

        // Převod IP adresy na bajty
        byte[] ipBytes = network.GetAddressBytes();

        // Sestavení IP adresy do 32-bit čísla (big-endian pořadí oktetů)
        uint ipValue = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) |
                     ((uint)ipBytes[2] << 8) | ipBytes[3];

        // Zarovnání na adresu sítě (zadaná adresa nemusí být adresou sítě)
        uint baseIp = ipValue & (0xFFFFFFFFu << hostBits);

        // Informace o skenované síti
        Console.WriteLine($"Skenování sítě {new IPAddress(ToBytes(baseIp))}/{cidr} ({hostCount} hostů)");

        // Seznam úloh s omezením paralelního běhu (max 50 současně)
        const int batchSize = 50;
        var tasks = new System.Collections.Generic.List<Task>(batchSize);

        // Cyklus přes všechny možné adresy hostitelů
        for (uint i = 1; i <= hostCount; i++)
        {
            // Spuštění asynchronní úlohy pro kontrolu hostitele
            tasks.Add(CheckHost(baseIp + i));

            // Po naplnění dávky čekání na dokončení úloh
            if (tasks.Count == batchSize)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        // Dokončení poslední neúplné dávky
        await Task.WhenAll(tasks);
    }

    // Převod 32-bit čísla na bajty IPv4 adresy ve správném pořadí
    static byte[] ToBytes(uint ip)
    {
        return new byte[] { (byte)(ip >> 24), (byte)(ip >> 16), (byte)(ip >> 8), (byte)ip };
    }

    static async Task CheckHost(uint ip)  // Asynchronní metoda pro kontrolu hostitele
    {
        // Převod čísla zpět na IPAddress objekt
        IPAddress address = new IPAddress(ToBytes(ip));

        // Vytvoření Ping objektu pomocí using pro automatické uvolnění zdrojů
        using (Ping ping = new Ping())
        {
            try
            {
                // Odeslání ping požadavku s timeoutem 1000ms
                PingReply reply = await ping.SendPingAsync(address, 1000);

                // Kontrola úspěšné odpovědi
                if (reply.Status == IPStatus.Success)
                {
                    // Výpis úspěšného nálezu
                    Console.WriteLine($"🟢 {address} - {reply.RoundtripTime}ms");

                    // Pokus o získání hostname pomocí DNS
                    try
                    {
                        IPHostEntry hostEntry = await Dns.GetHostEntryAsync(address);
                        Console.WriteLine($"   Hostname: {hostEntry.HostName}");
                    }
                    catch
                    {
                        // Výpis při chybě DNS dotazu
                        Console.WriteLine($"   Hostname: nepodařilo se získat");
                    }
                }
            }
            catch
            {
                // Potichu ignorovat chyby (např. nedostupný hostitel)
            }
        }
    }
}

/*
CIDR výpočty:
 hostBits = 32 - cidr - určuje počet bitů pro hostitele
 Počet použitelných adres je 2^hostBits - 2 (mínus adresa sítě a broadcast), proto se povoluje jen prefix /1 až /30
 Adresa se zarovná na adresu sítě pomocí AND s maskou a převádí se na bajty ve správném pořadí (ToBytes)
Paralelní zpracování:
 Seznam tasks slouží jako "okno" pro maximální počet současných pingů (50)
 Task.WhenAll(tasks) čeká na dokončení celé várky úloh a po cyklu i na poslední neúplnou várku
Metoda CheckHost:
 Používá asynchronní ping s timeoutem 1s
 Při úspěchu se pokusí o reverzní DNS lookup
 Všechny chyby jsou potichu zachyceny
Omezení zdrojů:
 using (Ping ping) zajišťuje správné uvolnění síťových zdrojů
 Limit paralelních úloh chrání před přetížením sítě

Program prochází všechny adresy v zadané síti, paralelně testuje jejich dostupnost a zobrazuje základní informace o aktivních hostitelích.
*/