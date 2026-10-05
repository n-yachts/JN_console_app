using System;  // Import základních systémových funkcí a tříd
using System.Net;  // Import síťových funkcí (IPAddress)
using System.Net.Sockets;  // Import AddressFamily pro kontrolu IPv4
using System.Text;  // Import StringBuilder pro skládání řádků

class BinaryConverter  // Hlavní třída - převod IPv4 adresy a masky do binárního tvaru
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje IP adresu s maskou (1 nebo 2 argumenty)
        if (args.Length < 1 || args.Length > 2)
        {
            Console.WriteLine("Použití: BinaryConverter <IP/prefix>");
            Console.WriteLine("         BinaryConverter <IP> <maska>");
            Console.WriteLine("         BinaryConverter <IP>            (bez masky, jen převod adresy)");
            Console.WriteLine("Příklad: BinaryConverter 192.168.1.77/26");
            Console.WriteLine("Příklad: BinaryConverter 10.1.2.3 255.255.0.0");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        // Zpracování vstupu: IP adresa a délka prefixu (-1 = maska nebyla zadána)
        if (!TryParseInput(args, out IPAddress ip, out int prefix))
        {
            Console.WriteLine("Neplatný vstup. Zadejte IPv4 adresu a masku (/0 až /32 nebo např. 255.255.255.0 - jen souvislá maska).");
            return;
        }

        uint address = ToUInt(ip);  // IP adresa jako 32bitové číslo

        Console.WriteLine();
        PrintRow("IP adresa", ip.ToString(), address, prefix);
        PrintHex(address);

        if (prefix >= 0)
        {
            uint mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);  // Posun o 32 bitů by se v C# neprovedl, proto zvláštní případ /0
            uint network = address & mask;  // Adresa sítě = adresa AND maska
            uint broadcast = network | ~mask;  // Broadcast = adresa sítě s nastavenými všemi hostitelskými bity

            Console.WriteLine();
            PrintRow("Maska", $"{FromUInt(mask)} (/{prefix})", mask, prefix);
            PrintRow("Adresa sítě", FromUInt(network).ToString(), network, prefix);
            PrintRow("Broadcast", FromUInt(broadcast).ToString(), broadcast, prefix);

            // Řádek s písmeny S (síťová část) a H (hostitelská část) - funguje i bez barev
            Console.WriteLine($"{"Část",-13}: {Marker(prefix)}");

            int hostBits = 32 - prefix;
            Console.WriteLine();
            Console.WriteLine($"Síťová část: {prefix} bitů (zeleně, S), hostitelská část: {hostBits} bitů (žlutě, H)");

            if (prefix <= 30)
            {
                // Použitelné adresy jsou mezi adresou sítě a broadcastem (obě krajní adresy se nepřidělují)
                long usable = (1L << hostBits) - 2;
                Console.WriteLine($"Použitelné adresy: {FromUInt(network + 1)} - {FromUInt(broadcast - 1)} ({usable} hostů)");
            }
            else if (prefix == 31)
            {
                Console.WriteLine("Použitelné adresy: 2 (spoj point-to-point podle RFC 3021)");
            }
            else
            {
                Console.WriteLine("Použitelné adresy: 1 (jediný host)");
            }
        }
    }

    // Zpracuje argumenty: "IP/prefix", "IP maska" nebo "IP /prefix"; prefix -1 znamená, že maska nebyla zadána
    static bool TryParseInput(string[] args, out IPAddress ip, out int prefix)
    {
        ip = null;
        prefix = -1;

        string ipText = args[0];
        string maskText = args.Length == 2 ? args[1] : null;

        // Forma IP/prefix v jediném argumentu
        if (args.Length == 1 && ipText.Contains("/"))
        {
            string[] parts = ipText.Split('/');
            if (parts.Length != 2) return false;
            ipText = parts[0];
            maskText = parts[1];
        }

        if (!IPAddress.TryParse(ipText, out ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        // Adresu "1.2" TryParse přijme jako 1.0.0.2, proto vyžadujeme přesně čtyři části
        if (ipText.Split('.').Length != 4)
            return false;

        if (maskText == null)
            return true;  // Bez masky

        maskText = maskText.TrimStart('/');

        if (int.TryParse(maskText, out int length) && !maskText.Contains("."))
        {
            if (length < 0 || length > 32) return false;
            prefix = length;
            return true;
        }

        // Maska v desítkovém tvaru - musí mít souvislé jedničky zleva
        if (IPAddress.TryParse(maskText, out IPAddress maskIp) &&
            maskIp.AddressFamily == AddressFamily.InterNetwork &&
            maskText.Split('.').Length == 4)
        {
            uint m = ToUInt(maskIp);
            uint inverted = ~m;
            if ((inverted & (inverted + 1)) != 0) return false;  // ~maska musí být tvaru 0...01...1

            int count = 0;
            for (int i = 31; i >= 0 && (m & (1u << i)) != 0; i--)
                count++;
            prefix = count;
            return true;
        }

        return false;
    }

    // Vypíše jeden řádek: popisek, desítkový tvar a pod ním binární tvar
    static void PrintRow(string label, string decimalText, uint value, int prefix)
    {
        Console.WriteLine($"{label,-13}: {decimalText}");
        Console.Write($"{"Binárně",-13}: ");
        PrintBinary(value, prefix);
        Console.WriteLine();
    }

    // Vypíše 32 bitů po oktetech oddělených tečkou; síťové bity zeleně, hostitelské žlutě
    static void PrintBinary(uint value, int prefix)
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

                if (prefix >= 0)
                    Console.ForegroundColor = bit < prefix ? ConsoleColor.Green : ConsoleColor.Yellow;

                Console.Write((value & (1u << (31 - bit))) != 0 ? '1' : '0');
            }
        }
        finally
        {
            Console.ForegroundColor = original;  // Vždy vrátit původní barvu
        }
    }

    // Hexadecimální zápis po oktetech, např. C0.A8.01.4D
    static void PrintHex(uint value)
    {
        byte[] b = FromUInt(value).GetAddressBytes();
        Console.WriteLine($"{"Hexadecimálně",-13}: {b[0]:X2}.{b[1]:X2}.{b[2]:X2}.{b[3]:X2}");
    }

    // Řádek se značkami S/H ve stejném rozložení jako binární zápis
    static string Marker(int prefix)
    {
        StringBuilder sb = new StringBuilder();
        for (int bit = 0; bit < 32; bit++)
        {
            if (bit > 0 && bit % 8 == 0) sb.Append('.');
            sb.Append(bit < prefix ? 'S' : 'H');
        }
        return sb.ToString();
    }

    static uint ToUInt(IPAddress ip)  // Čtyři bajty adresy (síťové pořadí) na číslo
    {
        byte[] b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    static IPAddress FromUInt(uint value)  // Číslo zpět na IP adresu
    {
        return new IPAddress(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
    }
}

/*
Struktura programu:
 Aplikace převádí IPv4 adresu (a volitelně masku) do binárního a hexadecimálního tvaru - pomůcka pro výuku subnettingu
 Maska může být zadána jako prefix (/26) i v desítkovém tvaru (255.255.255.192)
Klíčové komponenty:
 Main() - kontrola parametrů a výpis výsledků
 TryParseInput() - zpracování a validace vstupu (včetně kontroly souvislé masky)
 PrintBinary() - binární výpis s barevným rozlišením síťové (zelená) a hostitelské (žlutá) části
 Marker() - řádek s písmeny S/H, aby byla hranice vidět i bez barev nebo při přesměrování výstupu
Výpočty:
 Adresa sítě = adresa AND maska
 Broadcast = adresa sítě OR (NOT maska)
 Počet hostů = 2^(32 - prefix) - 2 (u /31 a /32 se krajní adresy nevylučují)
Výstupy:
 Adresa, maska, adresa sítě a broadcast v desítkovém i binárním tvaru
 Počet síťových a hostitelských bitů a rozsah použitelných adres
*/
