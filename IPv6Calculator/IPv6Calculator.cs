using System;  // Import základních systémových funkcí a tříd
using System.Net;  // Import síťových funkcí (IPAddress)
using System.Net.Sockets;  // Import AddressFamily pro kontrolu IPv6
using System.Numerics;  // Import BigInteger pro 128bitová čísla
using System.Text;  // Import StringBuilder

class IPv6Calculator  // Hlavní třída - rozbor IPv6 adres a prefixů
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Režim "eui64": sestavení adresy z MAC adresy
        if (args.Length >= 2 && args.Length <= 3 && args[0].Equals("eui64", StringComparison.OrdinalIgnoreCase))
        {
            Eui64(args[1], args.Length == 3 ? args[2] : null);
            return;
        }

        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: IPv6Calculator <IPv6[/prefix]>");
            Console.WriteLine("         IPv6Calculator eui64 <MAC> [prefix]");
            Console.WriteLine("Příklad: IPv6Calculator 2001:db8:acad:1::1/64");
            Console.WriteLine("Příklad: IPv6Calculator fe80::211:22ff:fe33:4455");
            Console.WriteLine("Příklad: IPv6Calculator eui64 00:11:22:33:44:55 2001:db8:acad:1::/64");
            return;
        }

        // Rozdělení vstupu na adresu a volitelný prefix (případná zóna za % se ignoruje)
        string[] parts = args[0].Split('/');
        string text = parts[0].Split('%')[0];
        int prefix = -1;  // -1 = prefix nebyl zadán

        if (parts.Length > 2 || (parts.Length == 2 && (!int.TryParse(parts[1], out prefix) || prefix < 0 || prefix > 128)))
        {
            Console.WriteLine("Neplatný prefix (0-128).");
            return;
        }

        if (!TryParseIPv6(text, out IPAddress ip))
        {
            Console.WriteLine("Neplatná IPv6 adresa.");
            return;
        }

        byte[] bytes = ip.GetAddressBytes();
        BigInteger value = ToNumber(bytes);

        Console.WriteLine();
        Console.WriteLine($"Plný tvar     : {Expand(bytes)}");
        Console.WriteLine($"Zkrácený tvar : {ip}");
        Console.WriteLine($"Typ adresy    : {Classify(bytes)}");

        if (IsMulticast(bytes))
        {
            Console.WriteLine($"Rozsah (scope): {MulticastScope(bytes[1] & 0x0F)}");
            string known = WellKnownMulticast(bytes);
            if (known != null)
                Console.WriteLine($"Známá skupina : {known}");
        }
        else if (!value.IsZero)
        {
            Console.WriteLine($"Interface ID  : {Group(bytes, 8)}:{Group(bytes, 10)}:{Group(bytes, 12)}:{Group(bytes, 14)}");

            // Adresa odvozená z MAC (SLAAC/EUI-64) obsahuje uprostřed interface ID bajty FF:FE
            if (bytes[11] == 0xFF && bytes[12] == 0xFE)
            {
                byte[] mac = { (byte)(bytes[8] ^ 0x02), bytes[9], bytes[10], bytes[13], bytes[14], bytes[15] };
                Console.WriteLine($"Možný EUI-64  : původní MAC {FormatMac(mac)}");
            }

            // Solicited-node multicast je ff02::1:ff + posledních 24 bitů adresy (používá ho Neighbor Discovery místo ARP)
            byte[] solicited = new byte[16];
            solicited[0] = 0xFF; solicited[1] = 0x02; solicited[11] = 0x01; solicited[12] = 0xFF;
            solicited[13] = bytes[13]; solicited[14] = bytes[14]; solicited[15] = bytes[15];
            Console.WriteLine($"Solicited-node: {new IPAddress(solicited)}");
        }

        if (prefix >= 0)
        {
            BigInteger hostMask = (BigInteger.One << (128 - prefix)) - 1;  // Jedničky na místě hostitelských bitů
            BigInteger network = value & ~hostMask;
            BigInteger last = network | hostMask;

            Console.WriteLine();
            Console.WriteLine($"Prefix        : /{prefix}");
            Console.WriteLine($"Síť           : {ToIp(network)}/{prefix}");
            Console.WriteLine($"První adresa  : {ToIp(network)}");
            Console.WriteLine($"Poslední adresa: {ToIp(last)}");
            Console.WriteLine($"Počet adres   : 2^{128 - prefix} = {BigInteger.One << (128 - prefix):N0}");

            // Standardní podsíť je /64; při kratším prefixu spočítáme, kolik /64 se do něj vejde
            if (prefix < 64)
                Console.WriteLine($"Podsítí /64   : {BigInteger.One << (64 - prefix):N0}");
            else if (prefix == 64)
                Console.WriteLine("Podsíť /64    : standardní velikost podsítě (64 bitů prefix + 64 bitů interface ID)");
            else
                Console.WriteLine("Poznámka      : prefix delší než /64 se na běžných sítích (SLAAC) nepoužívá; /127 a /128 se používají na spojích a loopbacku.");
        }
    }

    // Sestaví EUI-64 interface ID z MAC adresy a vypíše link-local a případně globální adresu
    static void Eui64(string macText, string prefixText)
    {
        // MAC může být zapsána s dvojtečkami, pomlčkami nebo tečkami (Cisco: 0011.2233.4455)
        string hex = macText.Replace(":", "").Replace("-", "").Replace(".", "");
        if (hex.Length != 12 || !long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _))
        {
            Console.WriteLine("Neplatná MAC adresa (očekává se 6 bajtů, např. 00:11:22:33:44:55 nebo 0011.2233.4455).");
            return;
        }

        byte[] mac = new byte[6];
        for (int i = 0; i < 6; i++)
            mac[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        // EUI-64: FF:FE se vloží doprostřed MAC a invertuje se sedmý bit (Universal/Local) prvního bajtu
        byte[] iid = { (byte)(mac[0] ^ 0x02), mac[1], mac[2], 0xFF, 0xFE, mac[3], mac[4], mac[5] };

        Console.WriteLine();
        Console.WriteLine($"MAC adresa    : {FormatMac(mac)}");
        Console.WriteLine($"EUI-64 ID     : {Group(iid, 0)}:{Group(iid, 2)}:{Group(iid, 4)}:{Group(iid, 6)}  (invertován bit U/L: {mac[0]:X2} -> {iid[0]:X2})");

        byte[] linkLocal = new byte[16];
        linkLocal[0] = 0xFE; linkLocal[1] = 0x80;
        Array.Copy(iid, 0, linkLocal, 8, 8);
        Console.WriteLine($"Link-local    : {new IPAddress(linkLocal)}");

        if (prefixText != null)
        {
            string[] parts = prefixText.Split('/');
            int prefixLength = 64;
            if (parts.Length > 2 || (parts.Length == 2 && (!int.TryParse(parts[1], out prefixLength) || prefixLength < 0 || prefixLength > 64)) ||
                !TryParseIPv6(parts[0], out IPAddress prefixIp))
            {
                Console.WriteLine("Neplatný prefix (očekává se IPv6 prefix délky nejvýše /64).");
                return;
            }

            byte[] global = prefixIp.GetAddressBytes();
            Array.Clear(global, 8, 8);  // Spodních 64 bitů nahradí interface ID
            Array.Copy(iid, 0, global, 8, 8);
            Console.WriteLine($"Globální adresa: {new IPAddress(global)}");
            Console.WriteLine($"Cisco příkaz  : ipv6 address {prefixText} eui-64");
        }
    }

    static bool TryParseIPv6(string text, out IPAddress ip)
    {
        // Řetězec bez dvojtečky by TryParse přijal jako IPv4, proto ho předem odmítneme
        return IPAddress.TryParse(text, out ip) && ip.AddressFamily == AddressFamily.InterNetworkV6 && text.Contains(":");
    }

    // Určí typ adresy podle prefixu (pořadí kontrol je důležité - konkrétnější prefixy dříve)
    static string Classify(byte[] b)
    {
        if (ToNumber(b).IsZero) return "Nespecifikovaná adresa (::/128)";
        if (ToNumber(b) == BigInteger.One) return "Loopback (::1/128)";
        if (IsMulticast(b)) return "Multicast (ff00::/8)";
        if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) return "Link-local unicast (fe80::/10)";
        if (b[0] == 0xFE && (b[1] & 0xC0) == 0xC0) return "Site-local (fec0::/10) - zastaralé";
        if ((b[0] & 0xFE) == 0xFC) return b[0] == 0xFD ? "Unique local, lokálně přidělená (fd00::/8)" : "Unique local (fc00::/7)";
        if (IsPrefix(b, 80, 0) && b[10] == 0xFF && b[11] == 0xFF) return "IPv4-mapped (::ffff:0:0/96)";
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8) return "Dokumentační (2001:db8::/32)";
        if (b[0] == 0x20 && b[1] == 0x02) return "6to4 (2002::/16)";
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00) return "Teredo (2001::/32)";
        if ((b[0] & 0xE0) == 0x20) return "Global unicast (2000::/3)";
        return "Rezervovaná / dosud nepřidělená";
    }

    static bool IsPrefix(byte[] b, int bits, byte value)  // Prvních 'bits' bitů je rovno 'value' (zde jen nuly)
    {
        for (int i = 0; i < bits / 8; i++)
            if (b[i] != value) return false;
        return true;
    }

    static bool IsMulticast(byte[] b) { return b[0] == 0xFF; }

    static string MulticastScope(int scope)  // Rozsah multicastu je dolní půlbajt druhého bajtu
    {
        switch (scope)
        {
            case 1: return "interface-local";
            case 2: return "link-local";
            case 4: return "admin-local";
            case 5: return "site-local";
            case 8: return "organization-local";
            case 14: return "global";
            default: return $"jiný ({scope})";
        }
    }

    static string WellKnownMulticast(byte[] b)  // Název nejběžnějších skupin
    {
        string a = new IPAddress(b).ToString();
        switch (a)
        {
            case "ff02::1": return "všechny uzly na linkce";
            case "ff02::2": return "všechny směrovače na linkce";
            case "ff02::5": return "OSPFv3 směrovače";
            case "ff02::6": return "OSPFv3 DR/BDR";
            case "ff02::9": return "RIPng směrovače";
            case "ff02::a": return "EIGRP směrovače";
            case "ff02::1:2": return "DHCPv6 relay agenti a servery";
            case "ff05::1:3": return "DHCPv6 servery (site)";
        }
        // Prefix ff02::1:ff00:0/104 používá Neighbor Discovery
        bool zeros = true;
        for (int i = 2; i < 11; i++) if (b[i] != 0) zeros = false;
        if (b[0] == 0xFF && b[1] == 0x02 && zeros && b[11] == 0x01 && b[12] == 0xFF)
            return "solicited-node multicast (Neighbor Discovery)";
        return null;
    }

    static BigInteger ToNumber(byte[] bytes)  // 16 bajtů na nezáporné 128bitové číslo
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    static IPAddress ToIp(BigInteger value)  // Číslo zpět na IPv6 adresu (doplněno nulami na 16 bajtů)
    {
        byte[] raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        byte[] bytes = new byte[16];
        Array.Copy(raw, 0, bytes, 16 - raw.Length, raw.Length);
        return new IPAddress(bytes);
    }

    static string Expand(byte[] b)  // Osm skupin po čtyřech hexadecimálních číslicích
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 16; i += 2)
        {
            if (i > 0) sb.Append(':');
            sb.Append(Group(b, i));
        }
        return sb.ToString();
    }

    static string Group(byte[] b, int index)  // Jedna skupina (dva bajty) jako čtyři hexadecimální číslice
    {
        return $"{b[index]:x2}{b[index + 1]:x2}";
    }

    static string FormatMac(byte[] mac)
    {
        return BitConverter.ToString(mac).Replace('-', ':');
    }
}

/*
Struktura programu:
 Aplikace rozebere IPv6 adresu (a volitelně prefix) - pomůcka pro výuku IPv6 adresování
 Režim eui64 sestaví interface ID a adresy z MAC adresy (SLAAC, příkaz "ipv6 address ... eui-64")
Zobrazované údaje:
 Plný a zkrácený tvar adresy, typ adresy (global unicast, link-local, unique local, multicast, ...)
 Interface ID a u adres odvozených z MAC (FF:FE uprostřed) původní MAC adresa
 Solicited-node multicast adresa pro danou unicast adresu
 U multicastu rozsah (scope) a název známé skupiny (OSPFv3, EIGRP, DHCPv6, ...)
 Při zadaném prefixu adresa sítě, první a poslední adresa, počet adres a počet podsítí /64
Výpočty:
 Adresy jsou 128bitová čísla (BigInteger), maska prefixu se vytvoří bitovými operacemi
 EUI-64: do MAC se doprostřed vloží FF:FE a invertuje se bit U/L (0x02) prvního bajtu
*/
