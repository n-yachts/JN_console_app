using System;  // Import základních systémových funkcí a tříd
using System.Collections.Generic;  // Import seznamů sítí a intervalů
using System.Linq;  // Import řazení a součtů
using System.Net;  // Import síťových funkcí (IPAddress)
using System.Net.Sockets;  // Import AddressFamily pro kontrolu IPv4
using System.Numerics;  // Import BitOperations pro počítání bitů
using System.Text;  // Import StringBuilder

class SummarizationTool  // Hlavní třída - sumarizace (agregace) IPv4 sítí
{
    // Jedna vstupní síť jako interval adres [Start, End]
    class Network
    {
        public ulong Start;  // Adresa sítě
        public ulong End;  // Poslední adresa sítě (broadcast)
        public int Prefix;  // Délka prefixu
    }

    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů - alespoň dvě sítě
        if (args.Length < 2)
        {
            Console.WriteLine("Použití: SummarizationTool <síť1/prefix> <síť2/prefix> [síť3/prefix ...]");
            Console.WriteLine("Příklad: SummarizationTool 192.168.0.0/24 192.168.1.0/24 192.168.2.0/24 192.168.3.0/24");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        List<Network> networks = new List<Network>();
        foreach (string arg in args)
        {
            if (!TryParseNetwork(arg, out Network n, out bool normalized))
            {
                Console.WriteLine($"Neplatná síť: {arg} (očekává se IPv4/prefix, např. 192.168.0.0/24).");
                return;
            }
            if (normalized)
                Console.WriteLine($"Poznámka: {arg} nemá nulové hostitelské bity, používám {ToIp(n.Start)}/{n.Prefix}.");
            networks.Add(n);
        }

        networks = networks.OrderBy(n => n.Start).ThenBy(n => n.Prefix).ToList();

        // Nejmenší jediný prefix, který pokryje všechny sítě: společné počáteční bity první a poslední adresy
        ulong first = networks.Min(n => n.Start);
        ulong last = networks.Max(n => n.End);
        uint difference = (uint)(first ^ last);
        int commonBits = difference == 0 ? 32 : BitOperations.LeadingZeroCount(difference);

        ulong summaryMask = commonBits == 0 ? 0UL : (0xFFFFFFFFUL << (32 - commonBits)) & 0xFFFFFFFFUL;
        ulong summaryStart = first & summaryMask;
        ulong summarySize = 1UL << (32 - commonBits);

        Console.WriteLine();
        Console.WriteLine("Vstupní sítě (zeleně společné bity):");
        foreach (Network n in networks)
        {
            Console.Write($"  {ToIp(n.Start) + "/" + n.Prefix,-20} ");
            PrintBinary(n.Start, commonBits);
            Console.WriteLine();
        }

        // Počet unikátních adres ve vstupních sítích (sloučení překrývajících se a sousedních intervalů)
        List<(ulong Start, ulong End)> merged = Merge(networks);
        ulong covered = 0;
        foreach (var m in merged)
            covered += m.End - m.Start + 1;

        Console.WriteLine();
        Console.WriteLine($"Společný prefix : {commonBits} bitů");
        Console.WriteLine($"Sumární trasa   : {ToIp(summaryStart)}/{commonBits}");
        Console.WriteLine($"Maska           : {ToIp(summaryMask)}");
        Console.WriteLine($"Wildcard maska  : {ToIp(~summaryMask & 0xFFFFFFFFUL)}");
        Console.WriteLine($"Pokrývá         : {summarySize} adres, vstupní sítě obsahují {covered} adres");

        if (covered == summarySize)
            Console.WriteLine("Sumarizace je přesná - pokrývá právě zadané sítě.");
        else
            Console.WriteLine($"Sumarizace není přesná - zahrnuje {summarySize - covered} adres navíc (směrovač by pro ně přijímal provoz, i když v sítích nejsou).");

        // Přesné minimální pokrytí: každý sloučený interval se rozloží na nejmenší počet zarovnaných bloků
        List<(ulong Start, int Prefix)> exact = new List<(ulong, int)>();
        foreach (var m in merged)
            exact.AddRange(RangeToCidr(m.Start, m.End));

        Console.WriteLine();
        Console.WriteLine($"Přesné minimální pokrytí ({exact.Count} {(exact.Count == 1 ? "trasa" : exact.Count < 5 ? "trasy" : "tras")}):");
        foreach (var e in exact)
            Console.WriteLine($"  {ToIp(e.Start)}/{e.Prefix}");

        Console.WriteLine();
        Console.WriteLine($"Cisco příkaz    : ip route {ToIp(summaryStart)} {ToIp(summaryMask)} <next-hop>");
    }

    // Zpracuje "IP/prefix"; adresu s nenulovými hostitelskými bity normalizuje na adresu sítě
    static bool TryParseNetwork(string text, out Network network, out bool normalized)
    {
        network = null;
        normalized = false;

        string[] parts = text.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
            parts[0].Split('.').Length != 4 || !int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 32)
            return false;

        byte[] b = ip.GetAddressBytes();
        ulong address = ((ulong)b[0] << 24) | ((ulong)b[1] << 16) | ((ulong)b[2] << 8) | b[3];
        ulong mask = prefix == 0 ? 0UL : (0xFFFFFFFFUL << (32 - prefix)) & 0xFFFFFFFFUL;
        ulong start = address & mask;
        normalized = start != address;

        network = new Network { Start = start, End = start + (1UL << (32 - prefix)) - 1, Prefix = prefix };
        return true;
    }

    // Seřazené intervaly sloučí - překrývající se i těsně navazující se spojí do jednoho
    static List<(ulong Start, ulong End)> Merge(List<Network> sorted)
    {
        List<(ulong Start, ulong End)> result = new List<(ulong, ulong)>();
        foreach (Network n in sorted)
        {
            if (result.Count > 0 && n.Start <= result[result.Count - 1].End + 1)
            {
                var lastItem = result[result.Count - 1];
                result[result.Count - 1] = (lastItem.Start, Math.Max(lastItem.End, n.End));
            }
            else
            {
                result.Add((n.Start, n.End));
            }
        }
        return result;
    }

    // Rozloží interval adres na nejmenší počet bloků zarovnaných na svou velikost (CIDR)
    static List<(ulong Start, int Prefix)> RangeToCidr(ulong start, ulong end)
    {
        List<(ulong, int)> blocks = new List<(ulong, int)>();
        while (start <= end)
        {
            // Největší blok, který začíná na 'start' (omezený zarovnáním) a nepřesáhne 'end'
            ulong size = start == 0 ? (1UL << 32) : (start & (~start + 1));
            while (size > end - start + 1)
                size >>= 1;

            blocks.Add((start, 32 - (int)Math.Log2(size)));
            start += size;
        }
        return blocks;
    }

    // Vypíše 32 bitů po oktetech; společné bity zeleně, ostatní šedě
    static void PrintBinary(ulong value, int commonBits)
    {
        ConsoleColor original = Console.ForegroundColor;
        try
        {
            for (int bit = 0; bit < 32; bit++)
            {
                if (bit > 0 && bit % 8 == 0)
                {
                    Console.ForegroundColor = original;
                    Console.Write('.');
                }
                Console.ForegroundColor = bit < commonBits ? ConsoleColor.Green : ConsoleColor.DarkGray;
                Console.Write((value & (1UL << (31 - bit))) != 0 ? '1' : '0');
            }
        }
        finally
        {
            Console.ForegroundColor = original;
        }
    }

    static IPAddress ToIp(ulong value)  // Číslo zpět na IP adresu
    {
        return new IPAddress(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
    }
}

/*
Struktura programu:
 Aplikace sloučí seznam IPv4 sítí do co nejmenšího počtu sumárních tras - pomůcka pro výuku sumarizace (route summarization)
Postup výpočtu:
 Jediná sumární trasa = společné počáteční bity první a poslední adresy ze všech sítí (nejmenší zarovnaný blok, který vše pokryje)
 Přesnost sumarizace se zjistí porovnáním velikosti bloku s počtem unikátních adres ve vstupních sítích
 Přesné minimální pokrytí: překrývající se a navazující sítě se sloučí do intervalů a každý interval se rozloží na nejmenší počet zarovnaných bloků
Výstupy:
 Binární zobrazení vstupních sítí se zvýrazněnými společnými bity
 Sumární trasa s maskou a wildcard maskou, informace zda je přesná, seznam přesných tras a ukázka příkazu ip route
*/
