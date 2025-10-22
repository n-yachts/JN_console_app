using System;
using System.Net;

class SubnetCalculator
{
    static void Main(string[] args)
    {
        // Kontrola počtu vstupních argumentů
        if (args.Length != 2)
        {
            // Nápověda při nesprávném použití
            Console.WriteLine("Použití: SubnetCalculator <IP/maska> <počet hostů>");
            Console.WriteLine("Příklad: SubnetCalculator 192.168.1.0/24 50");
            return;  // Ukončení programu
        }

        // Rozdělení vstupu na IP adresu a část s maskou
        string[] ipParts = args[0].Split('/');

        // Parsování IP adresy ze vstupu
        IPAddress ip = IPAddress.Parse(ipParts[0]);

        // Převod masky z CIDR notace na celé číslo
        int maskBits = int.Parse(ipParts[1]);

        // Získání požadovaného počtu hostů
        int requiredHosts = int.Parse(args[1]);

        // Výpočet nové masky:
        // 1. Přidání 2 (síťová + broadcastová adresa)
        // 2. Logaritmus o základu 2 pro zjištění potřebných bitů
        // 3. Zaokrouhlení nahoru (nelze mít část bitů)
        // 4. Odečtení od 32 (celkový počet bitů)
        int newMaskBits = 32 - (int)Math.Ceiling(Math.Log(requiredHosts + 2, 2));

        // Vytvoření masky jako 32-bitového čísla:
        // 0xFFFFFFFF = 255.255.255.255
        // Bitový posun vlevo vyplní jedničkami síťovou část
        uint mask = 0xFFFFFFFFu << (32 - newMaskBits);

        // Výpis výsledků
        Console.WriteLine($"Původní síť: {ip}/{maskBits}");
        Console.WriteLine($"Požadovaných hostů: {requiredHosts}");
        Console.WriteLine($"Nová maska: /{newMaskBits}");
        Console.WriteLine($"Skutečná kapacita: {Math.Pow(2, 32 - newMaskBits) - 2} hostů");
    }
}

/*
CIDR notace:
 Formát IP/maska (např. 192.168.1.0/24)
 Maska udává počet bitů síťové části
Výpočet masky:
 Vzorec: 32 - ceil(log2(hosts + 2))
 +2 zahrnuje síťovou a broadcastovou adresu
 Příklad pro 50 hostů: 32 - ceil(log2(52)) ≈ 32 - 6 = /26
Bitové operace:
 0xFFFFFFFF reprezentuje 32 bitů
 Posun << (32 - newMaskBits) vytvoří masku
 Příklad: /26 = 255.255.255.192
Kapacita sítě:
 Počet použitelných adres: 2^(hostovská část) - 2
 -2 odečítá síťovou a broadcastovou adresu

Tento kód vypočítá minimální velikost podsítě, která pojme zadaný počet hostů, ale neimplementuje výpočet konkrétních IP adres nebo rozdělení do větších sítí.
*/