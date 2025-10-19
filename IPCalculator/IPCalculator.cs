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

        IPAddress ipAddress = IPAddress.Parse(parts[0]);  // Převedení řetězce na IPAddress objekt
        int maskLength = int.Parse(parts[1]);  // Převedení délky masky na číslo

        // Výpočet masky sítě pomocí bitového posunu
        uint mask = 0xFFFFFFFFu << (32 - maskLength);  // Vytvoření bitové masky
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
        byte[] firstUsableBytes = networkBytes;  // Začneme od síťové adresy
        firstUsableBytes[3] += 1;  // Inkrementujeme poslední bajt
        IPAddress firstUsable = new IPAddress(firstUsableBytes);

        // Výpočet poslední použitelné adresy
        byte[] lastUsableBytes = broadcastBytes;  // Začneme od broadcast adresy
        lastUsableBytes[3] -= 1;  // Dekrementujeme poslední bajt
        IPAddress lastUsable = new IPAddress(lastUsableBytes);

        // Výpis všech vypočtených hodnot
        Console.WriteLine($"IP adresa: {ipAddress}");
        Console.WriteLine($"Maska sítě: {subnetMask} (/{maskLength})");
        Console.WriteLine($"Adresa sítě: {networkAddress}");
        Console.WriteLine($"Broadcast: {broadcastAddress}");
        Console.WriteLine($"Rozsah hostů: {firstUsable} - {lastUsable}");
        // Výpočet počtu hostů: 2^(počet volných bitů) - 2 (síť + broadcast)
        Console.WriteLine($"Počet hostů: {Math.Pow(2, 32 - maskLength) - 2}");
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
Program pracuje správně pro IPv4 adresy s maskou v CIDR zápisu
*/