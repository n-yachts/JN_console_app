using System;  // Import základních systémových funkcí a tříd
using System.Collections.Generic;  // Import seznamů pro požadavky a přidělené podsítě
using System.Linq;  // Import řazení požadavků
using System.Net;  // Import síťových funkcí (IPAddress)
using System.Net.Sockets;  // Import AddressFamily pro kontrolu IPv4

class VLSMCalculator  // Hlavní třída - rozdělení sítě na podsítě různých velikostí (VLSM)
{
    // Jeden požadavek na podsíť: původní pořadí zadání a počet požadovaných hostů
    class Request
    {
        public int Number;  // Pořadí v zadání (od 1)
        public long Hosts;  // Požadovaný počet použitelných adres
        public int HostBits;  // Počet hostitelských bitů potřebných pro požadavek
    }

    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů - síť a alespoň jeden požadavek na počet hostů
        if (args.Length < 2)
        {
            Console.WriteLine("Použití: VLSMCalculator <síť/prefix> <hostů1> [hostů2 ...]");
            Console.WriteLine("Příklad: VLSMCalculator 192.168.0.0/24 100 50 25 10");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        // Zpracování základní sítě (např. 192.168.0.0/24)
        string[] parts = args[0].Split('/');
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
            parts[0].Split('.').Length != 4 ||
            !int.TryParse(parts[1], out int basePrefix) || basePrefix < 0 || basePrefix > 30)
        {
            Console.WriteLine("Neplatná síť. Použijte IPv4 adresu a prefix /0 až /30, např. 192.168.0.0/24.");
            return;
        }

        ulong baseMask = basePrefix == 0 ? 0UL : (0xFFFFFFFFUL << (32 - basePrefix)) & 0xFFFFFFFFUL;
        ulong start = ToNumber(ip) & baseMask;  // Začátek sítě (adresa sítě)
        ulong total = 1UL << (32 - basePrefix);  // Celkový počet adres v zadané síti

        if (start != ToNumber(ip))
            Console.WriteLine($"Poznámka: adresa {ip} není adresou sítě, používám {ToIp(start)}/{basePrefix}.\n");

        // Zpracování požadavků na počet hostů
        List<Request> requests = new List<Request>();
        for (int i = 1; i < args.Length; i++)
        {
            if (!long.TryParse(args[i], out long hosts) || hosts < 1 || hosts > (long)total)
            {
                Console.WriteLine($"Neplatný počet hostů: {args[i]} (musí být kladné číslo, nejvýše velikost sítě).");
                return;
            }

            // Hledáme nejmenší počet hostitelských bitů, aby použitelných adres (2^h - 2) bylo dost; nejméně 2 bity (/30)
            int hostBits = 2;
            while ((1L << hostBits) - 2 < hosts)
                hostBits++;

            requests.Add(new Request { Number = i, Hosts = hosts, HostBits = hostBits });
        }

        Console.WriteLine($"VLSM pro {ToIp(start)}/{basePrefix} ({total} adres)\n");
        Console.WriteLine("Přidělování od největšího požadavku (zarovnání na hranici velikosti podsítě):\n");
        Console.WriteLine($"{"#",-3} {"Požadavek",-10} {"Kapacita",-9} {"Podsíť",-20} {"Maska",-16} {"Použitelné adresy",-33} {"Broadcast",-15}");
        Console.WriteLine(new string('-', 112));

        ulong cursor = start;  // Ukazatel na první dosud nepřidělenou adresu
        ulong end = start + total;  // První adresa za sítí
        ulong allocated = 0;  // Součet velikostí přidělených podsítí
        List<Request> failed = new List<Request>();

        // Nejdříve největší podsítě, aby zarovnání nezpůsobovalo zbytečné mezery
        foreach (Request r in requests.OrderByDescending(x => x.HostBits).ThenBy(x => x.Number))
        {
            ulong size = 1UL << r.HostBits;
            ulong aligned = (cursor + size - 1) / size * size;  // Zarovnání nahoru na násobek velikosti podsítě

            if (aligned + size > end)
            {
                failed.Add(r);  // Podsíť se do zbývajícího místa nevejde
                continue;
            }

            int prefix = 32 - r.HostBits;
            ulong mask = (0xFFFFFFFFUL << r.HostBits) & 0xFFFFFFFFUL;
            Console.WriteLine($"{r.Number,-3} {r.Hosts,-10} {size - 2,-9} {ToIp(aligned) + "/" + prefix,-20} {ToIp(mask),-16} " +
                              $"{ToIp(aligned + 1) + " - " + ToIp(aligned + size - 2),-33} {ToIp(aligned + size - 1),-15}");

            allocated += size;
            cursor = aligned + size;
        }

        Console.WriteLine();

        foreach (Request r in failed)
            Console.WriteLine($"Požadavek č. {r.Number} ({r.Hosts} hostů, potřeba /{32 - r.HostBits}) se do sítě už nevejde.");

        Console.WriteLine($"Přiděleno: {allocated} z {total} adres ({allocated * 100.0 / total:F1} %)");
        Console.WriteLine(cursor < end
            ? $"Volné místo začíná na adrese {ToIp(cursor)} ({end - cursor} adres)"
            : "Síť je zcela vyčerpána.");
    }

    static ulong ToNumber(IPAddress ip)  // Čtyři bajty adresy (síťové pořadí) na číslo
    {
        byte[] b = ip.GetAddressBytes();
        return ((ulong)b[0] << 24) | ((ulong)b[1] << 16) | ((ulong)b[2] << 8) | b[3];
    }

    static IPAddress ToIp(ulong value)  // Číslo zpět na IP adresu
    {
        return new IPAddress(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
    }
}

/*
Struktura programu:
 Aplikace rozdělí zadanou síť na podsítě různých velikostí (VLSM - Variable Length Subnet Mask) podle požadovaného počtu hostů
 Navazuje na SubnetCalculator, který počítá jen jednu podsíť
Postup výpočtu:
 Pro každý požadavek se najde nejmenší počet hostitelských bitů h, aby 2^h - 2 >= požadovaný počet hostů (nejméně /30)
 Podsítě se přidělují od největší k nejmenší a každá začíná na adrese zarovnané na svou velikost
 Požadavek, který se do zbývajícího místa nevejde, se vypíše jako chyba
Výstupy:
 Tabulka podsítí (adresa a prefix, maska, rozsah použitelných adres, broadcast) v pořadí přidělení
 Počet přidělených adres, využití sítě a začátek volného místa
*/
