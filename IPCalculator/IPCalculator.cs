using System;  // Import základních systémových knihoven
using System.Linq;  // Import pro LINQ (použito pro Reverse())
using System.Net;  // Import pro práci s IP adresami

class IPCalculator  // Hlavní třída programu
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: IPCalculator <IP/maska>");
            Console.WriteLine("Příklad: IPCalculator 192.168.1.0/24");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        // Rozdělení vstupního argumentu na část IP adresy a masky
        string[] parts = args[0].Split('/');
        if (parts.Length != 2)
        {
            Console.WriteLine("Neplatný formát. Použijte formát IP/maska.");
            return;  // Ukončení programu při chybném formátu
        }

        // Kontrola vstupu: IPv4 adresa a délka masky 0-32
        if (!IPAddress.TryParse(parts[0], out IPAddress ipAddress) ||
            ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
            !int.TryParse(parts[1], out int maskLength) || maskLength < 0 || maskLength > 32)
        {
            Console.WriteLine("Neplatný vstup. Použijte IPv4 adresu a masku 0-32.");
            return;
        }

        // Výpočet masky sítě pomocí bitového posunu (posun o 32 bitů by se v C# neprovedl, proto zvláštní případ /0)
        uint mask = maskLength == 0 ? 0u : 0xFFFFFFFFu << (32 - maskLength);  // Vytvoření bitové masky
        // Konverze na IPAddress (Reverse je potřeba kvůli odlišnému pořadí bajtů)
        IPAddress subnetMask = new IPAddress(BitConverter.GetBytes(mask).Reverse().ToArray());

        // Výpočet adresy sítě
        byte[] ipBytes = ipAddress.GetAddressBytes();  // Získání bajtů IP adresy
        byte[] maskBytes = subnetMask.GetAddressBytes();  // Získání bajtů masky
        byte[] networkBytes = new byte[4];  // Příprava pole pro síťovou adresu
        for (int i = 0; i < 4; i++)
        {
            // Aplikace masky na každý bajt pomocí AND operace
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
        }
        IPAddress networkAddress = new IPAddress(networkBytes);  // Vytvoření síťové adresy

        // Výpočet broadcast adresy
        byte[] broadcastBytes = new byte[4];  // Příprava pole pro broadcast
        for (int i = 0; i < 4; i++)
        {
            // Kombinace síťové adresy s inverzní maskou pomocí OR operace
            broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
        }
        IPAddress broadcastAddress = new IPAddress(broadcastBytes);

        // Výpočet první použitelné adresy
        byte[] firstUsableBytes = (byte[])networkBytes.Clone();  // Kopie, aby se nezměnila síťová adresa
        byte[] lastUsableBytes = (byte[])broadcastBytes.Clone();  // Kopie, aby se nezměnila broadcast adresa

        // Pro /31 a /32 neexistuje síťová ani broadcast adresa v klasickém smyslu (RFC 3021)
        if (maskLength < 31)
        {
            firstUsableBytes[3] += 1;  // Inkrementujeme poslední bajt
            lastUsableBytes[3] -= 1;   // Dekrementujeme poslední bajt
        }
        IPAddress firstUsable = new IPAddress(firstUsableBytes);

        // Výpočet poslední použitelné adresy
        IPAddress lastUsable = new IPAddress(lastUsableBytes);

        // Výpis všech vypočtených hodnot
        Console.WriteLine($"IP adresa: {ipAddress}");
        Console.WriteLine($"Maska sítě: {subnetMask} (/{maskLength})");
        Console.WriteLine($"Adresa sítě: {networkAddress}");
        Console.WriteLine($"Broadcast: {broadcastAddress}");
        Console.WriteLine($"Rozsah hostů: {firstUsable} - {lastUsable}");
        // Výpočet počtu hostů: 2^(počet volných bitů) - 2 (síť + broadcast)
        long hostCount = maskLength >= 31 ? (maskLength == 32 ? 1 : 2) : (1L << (32 - maskLength)) - 2;
        Console.WriteLine($"Počet hostů: {hostCount}");
    }
}

/*
Bitová maska:
 0xFFFFFFFFu představuje 32 bitů plně zapnutých
 Posun << (32 - maskLength) vytvoří požadovanou masku
Síťová adresa:
 Vzniká aplikací masky na IP adresu pomocí operace AND
 Např.: 192.168.1.100 & 255.255.255.0 = 192.168.1.0
Broadcast adresa:
 Vzniká kombinací síťové adresy s inverzní maskou pomocí OR
 Např.: 192.168.1.0 | 0.0.0.255 = 192.168.1.255
Použitelné adresy:
 První: síťová adresa + 1 (192.168.1.1)
 Poslední: broadcast adresa - 1 (192.168.1.254)
Výjimky: u masky /31 (spoj dvou zařízení, RFC 3021) a /32 (jediný host) se použitelné adresy neposouvají o 1
Počet hostů: 2^(32 - maska) - 2; pro /31 jsou to 2 adresy, pro /32 jedna
Program pracuje s IPv4 adresami a maskou v CIDR zápisu
*/