using System;  // Import základních systémových funkcí a tříd
using System.Collections.Generic;  // Import seznamu pro výpis adres
using System.Net;  // Import síťových funkcí (IPAddress)
using System.Net.Sockets;  // Import AddressFamily pro kontrolu IPv4
using System.Numerics;  // Import BitOperations pro počítání bitů
using System.Text;  // Import StringBuilder

class WildcardMaskCalculator  // Hlavní třída - převod mezi maskou a wildcard maskou pro ACL
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Režim "wildcard": rozbor zadané wildcard masky
        if (args.Length == 3 && args[0].Equals("wildcard", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseIp(args[1], out uint ip) || !TryParseIp(args[2], out uint wildcard))
            {
                Console.WriteLine("Neplatná IPv4 adresa nebo wildcard maska.");
                return;
            }
            ExplainWildcard(ip, wildcard);
            return;
        }

        // Kontrola počtu argumentů pro režim převodu masky
        if (args.Length < 1 || args.Length > 2)
        {
            Console.WriteLine("Použití: WildcardMaskCalculator <IP/prefix>");
            Console.WriteLine("         WildcardMaskCalculator <IP> <maska>");
            Console.WriteLine("         WildcardMaskCalculator wildcard <IP> <wildcard>   (rozbor wildcard masky)");
            Console.WriteLine("Příklad: WildcardMaskCalculator 192.168.1.0/26");
            Console.WriteLine("Příklad: WildcardMaskCalculator 10.0.0.0 255.255.248.0");
            Console.WriteLine("Příklad: WildcardMaskCalculator wildcard 192.168.1.0 0.0.0.254");
            return;
        }

        if (!TryParseNetwork(args, out uint address, out int prefix, out string error))
        {
            Console.WriteLine(error);
            return;
        }

        uint mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);
        uint wild = ~mask;  // Wildcard maska je inverze masky (1 = bit se nekontroluje)
        uint network = address & mask;
        string net = Format(network);
        string wc = Format(wild);

        Console.WriteLine();
        Console.WriteLine($"IP adresa     : {Format(address)}");
        Console.WriteLine($"Maska         : {Format(mask)} (/{prefix})");
        Console.WriteLine($"Wildcard maska: {wc}");
        Console.WriteLine($"Binárně maska : {Binary(mask)}");
        Console.WriteLine($"Binárně wild. : {Binary(wild)}");
        Console.WriteLine($"Adresa sítě   : {net}");
        Console.WriteLine($"Počet adres   : {1L << (32 - prefix)}");

        Console.WriteLine();
        Console.WriteLine("Příklady použití na Cisco zařízení:");

        // Pro /32 se v ACL používá klíčové slovo host, pro /0 klíčové slovo any
        string standard = prefix == 32 ? $"host {net}" : prefix == 0 ? "any" : $"{net} {wc}";
        Console.WriteLine($"  Standardní ACL : access-list 10 permit {standard}");
        Console.WriteLine($"  Rozšířená ACL  : permit ip {standard} any");
        Console.WriteLine($"  OSPF           : network {net} {wc} area 0");
        Console.WriteLine($"  EIGRP          : network {net} {wc}");
    }

    // Rozbor wildcard masky: kolik adres odpovídá, zda je souvislá a jaké adresy patří do výběru
    static void ExplainWildcard(uint ip, uint wildcard)
    {
        uint fixedMask = ~wildcard;  // Bity, které se musí shodovat (0 ve wildcard masce)
        uint baseAddress = ip & fixedMask;  // Nejnižší adresa výběru
        int ignored = BitOperations.PopCount(wildcard);  // Počet ignorovaných bitů
        long count = 1L << ignored;
        bool contiguous = (wildcard & (wildcard + 1)) == 0;  // Souvislá = tvar 0...01...1

        Console.WriteLine();
        Console.WriteLine($"IP adresa      : {Format(ip)}");
        Console.WriteLine($"Wildcard maska : {Format(wildcard)}");
        Console.WriteLine($"Binárně adresa : {Binary(ip)}");
        Console.WriteLine($"Binárně wild.  : {Binary(wildcard)}");
        Console.WriteLine($"Kontrola bitů  : {CheckRow(wildcard)}   (X = musí se shodovat, * = ignoruje se)");
        Console.WriteLine();
        Console.WriteLine($"Počet odpovídajících adres: {count}");

        if (contiguous)
        {
            Console.WriteLine($"Souvislá wildcard maska - odpovídá síti {Format(baseAddress)}/{32 - ignored}");
            Console.WriteLine($"Rozsah: {Format(baseAddress)} - {Format(baseAddress | wildcard)}");
        }
        else
        {
            Console.WriteLine("Nesouvislá wildcard maska - nelze ji vyjádřit jako jedinou síť (např. výběr lichých nebo sudých adres).");
            Console.WriteLine("První odpovídající adresy:");

            // Procházení všech podmnožin bitů wildcard masky v rostoucím pořadí
            uint sub = 0;
            for (int i = 0; i < 16 && i < count; i++)
            {
                Console.WriteLine($"  {Format(baseAddress | sub)}");
                sub = (sub - wildcard) & wildcard;
            }
            if (count > 16)
                Console.WriteLine($"  ... a dalších {count - 16}");
        }
    }

    // Zpracuje "IP/prefix" nebo "IP maska"; masku akceptuje jen souvislou (pro wildcard slouží režim wildcard)
    static bool TryParseNetwork(string[] args, out uint address, out int prefix, out string error)
    {
        address = 0;
        prefix = 0;
        error = "Neplatný vstup. Použijte IP/prefix (0-32) nebo IP a souvislou masku.";

        string ipText = args[0];
        string maskText = args.Length == 2 ? args[1] : null;

        if (args.Length == 1)
        {
            string[] parts = ipText.Split('/');
            if (parts.Length != 2) return false;
            ipText = parts[0];
            maskText = parts[1];
        }

        if (!TryParseIp(ipText, out address)) return false;

        maskText = maskText.TrimStart('/');
        if (!maskText.Contains("."))
            return int.TryParse(maskText, out prefix) && prefix >= 0 && prefix <= 32;

        if (!TryParseIp(maskText, out uint mask)) return false;

        uint inverted = ~mask;
        if ((inverted & (inverted + 1)) != 0)
        {
            error = "Maska není souvislá. Pro rozbor wildcard masky použijte: WildcardMaskCalculator wildcard <IP> <wildcard>";
            return false;
        }

        prefix = BitOperations.PopCount(mask);
        return true;
    }

    static bool TryParseIp(string text, out uint value)
    {
        value = 0;
        if (!IPAddress.TryParse(text, out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork || text.Split('.').Length != 4)
            return false;
        byte[] b = ip.GetAddressBytes();
        value = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        return true;
    }

    static string Format(uint value)  // Číslo na čtyři oktety oddělené tečkou
    {
        return $"{value >> 24}.{(value >> 16) & 255}.{(value >> 8) & 255}.{value & 255}";
    }

    static string Binary(uint value)  // 32 bitů po oktetech oddělených tečkou
    {
        StringBuilder sb = new StringBuilder();
        for (int bit = 0; bit < 32; bit++)
        {
            if (bit > 0 && bit % 8 == 0) sb.Append('.');
            sb.Append((value & (1u << (31 - bit))) != 0 ? '1' : '0');
        }
        return sb.ToString();
    }

    static string CheckRow(uint wildcard)  // X u bitů, které se kontrolují, * u ignorovaných
    {
        StringBuilder sb = new StringBuilder();
        for (int bit = 0; bit < 32; bit++)
        {
            if (bit > 0 && bit % 8 == 0) sb.Append('.');
            sb.Append((wildcard & (1u << (31 - bit))) != 0 ? '*' : 'X');
        }
        return sb.ToString();
    }
}

/*
Struktura programu:
 Aplikace převádí masku podsítě na wildcard masku, kterou používají přístupové seznamy (ACL) a příkaz network v OSPF a EIGRP
 Wildcard maska je inverzí masky: 0 = bit se musí shodovat, 1 = bit se ignoruje
Režimy:
 IP/prefix nebo IP maska - vypíše masku, wildcard masku, adresu sítě a hotové řádky pro ACL, OSPF a EIGRP
 wildcard IP wildcard - rozbor zadané wildcard masky (počet odpovídajících adres, rozsah nebo výpis adres u nesouvislé masky)
Pravidla zápisu v ACL:
 Prefix /32 se zapisuje klíčovým slovem host, prefix /0 klíčovým slovem any
 Wildcard maska může být nesouvislá, např. 0.0.0.254 vybere všechny sudé nebo liché adresy
Výstupy:
 Desítkové i binární zobrazení masky a wildcard masky, adresa sítě, počet adres a ukázky příkazů
*/
