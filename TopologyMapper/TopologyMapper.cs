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

        // Parsování IP adresy sítě z první části
        IPAddress network = IPAddress.Parse(parts[0]);

        // Parsování CIDR notace z druhé části
        int cidr = int.Parse(parts[1]);

        // Výpočet počtu bitů pro hostitele
        int hostBits = 32 - cidr;

        // Výpočet počtu hostitelů v síti (odečítáme síť a broadcast)
        uint hostCount = (uint)Math.Pow(2, hostBits) - 2;

        // Informace o skenované síti
        Console.WriteLine($"Skenování sítě {network}/{cidr} ({hostCount} hostů)");

        // Převod IP adresy na bajty
        byte[] ipBytes = network.GetAddressBytes();

        // Sestavení základní IP adresy do 32-bit čísla
        uint baseIp = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) |
                     ((uint)ipBytes[2] << 8) | ipBytes[3];

        // Vytvoření pole úloh s omezením paralelního běhu (max 50 současně)
        var tasks = new Task[Math.Min(50, hostCount)];

        // Cyklus přes všechny možné adresy hostitelů
        for (uint i = 1; i <= hostCount; i++)
        {
            // Výpočet konkrétní IP adresy
            uint ip = baseIp + i;

            // Spuštění asynchronní úlohy pro kontrolu hostitele
            var task = CheckHost(ip);

            // Přiřazení úlohy do pole (cyklické použití indexů)
            tasks[(i - 1) % tasks.Length] = task;

            // Po naplnění pole úloh čekání na jejich dokončení
            if (i % tasks.Length == 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }

    static async Task CheckHost(uint ip)  // Asynchronní metoda pro kontrolu hostitele
    {
        // Převod čísla zpět na IPAddress objekt
        IPAddress address = new IPAddress(ip);

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
 Math.Pow(2, hostBits) - 2 - vypočítá počet dostupných adres (mínus síť a broadcast)
Paralelní zpracování:
 Pole tasks slouží jako "okno" pro maximální počet současných pingů
 Indexování (i-1) % tasks.Length cyklicky plní pole úlohami
 Task.WhenAll(tasks) čeká na dokončení celé várky úloh
Metoda CheckHost:
 Používá asynchronní ping s timeoutem 1s
 Při úspěchu se pokusí o reverzní DNS lookup
 Všechny chyby jsou potichu zachyceny
Omezení zdrojů:
 using (Ping ping) zajišťuje správné uvolnění síťových zdrojů
 Limit paralelních úloh chrání před přetížením sítě

Program prochází všechny adresy v zadané síti, paralelně testuje jejich dostupnost a zobrazuje základní informace o aktivních hostitelích.
*/