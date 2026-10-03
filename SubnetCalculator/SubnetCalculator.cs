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
        // Kontrola vstupu: IP adresa, maska 0-32 a kladný počet hostů
        if (ipParts.Length != 2 || !IPAddress.TryParse(ipParts[0], out IPAddress ip) ||
            !int.TryParse(ipParts[1], out int maskBits) || maskBits < 0 || maskBits > 32 ||
            !int.TryParse(args[1], out int requiredHosts) || requiredHosts < 1)
        {
            Console.WriteLine("Neplatný vstup. Příklad: SubnetCalculator 192.168.1.0/24 50");
            return;
        }

        // Výpočet nové masky:
        // 1. Přidání 2 (síťová + broadcastová adresa)
        // 2. Logaritmus o základu 2 pro zjištění potřebných bitů
        // 3. Zaokrouhlení nahoru (nelze mít část bitů)
        // 4. Odečtení od 32 (celkový počet bitů)
        // (počet bitů se zjišťuje celočíselně, aby Math.Log nezpůsobil chybu zaokrouhlení u mocnin dvou)
        int hostBits = 0;
        while ((1L << hostBits) < (long)requiredHosts + 2)
            hostBits++;
        int newMaskBits = 32 - hostBits;

        if (newMaskBits < maskBits)
        {
            Console.WriteLine($"Požadovaný počet hostů se do sítě /{maskBits} nevejde.");
            return;
        }

        // Vytvoření masky jako 32-bitového čísla (pro /0 je maska 0 - posun o 32 bitů by v C# nic neudělal)
        uint mask = newMaskBits == 0 ? 0u : 0xFFFFFFFFu << (32 - newMaskBits);

        // Výpis výsledků
        Console.WriteLine($"Původní síť: {ip}/{maskBits}");
        Console.WriteLine($"Požadovaných hostů: {requiredHosts}");
        Console.WriteLine($"Nová maska: /{newMaskBits}");
        Console.WriteLine($"Maska: {new IPAddress(new[] { (byte)(mask >> 24), (byte)(mask >> 16), (byte)(mask >> 8), (byte)mask })}");
        Console.WriteLine($"Skutečná kapacita: {(1L << hostBits) - 2} hostů");
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