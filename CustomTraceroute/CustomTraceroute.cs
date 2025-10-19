using System;  // Základní jmenný prostor pro vstup/výstup a základní funkce
using System.Net;  // Jmenný prostor pro práci se sítí (IP adresy, stavy)
using System.Net.NetworkInformation;  // Obsahuje třídy pro síťové operace (Ping, PingOptions)
using System.Threading.Tasks;  // Podpora asynchronních operací

class CustomTraceroute  // Hlavní třída pro provedení traceroute
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda
    {
        if (args.Length != 1)  // Kontrola počtu argumentů
        {
            Console.WriteLine("Použití: CustomTraceroute <cíl>");  // Chybová zpráva
            return;  // Ukončení programu při chybném počtu argumentů
        }

        string target = args[0];  // Uložení cílové adresy z argumentu
        int maxHops = 30;  // Maximální počet skoků (TTL)
        int timeout = 1000;  // Časový limit pro odpověď v milisekundách

        using (Ping ping = new Ping())  // Vytvoření ping objektu s automatickým uvolněním zdrojů
        {
            for (int ttl = 1; ttl <= maxHops; ttl++)  // Smyčka přes všechny TTL od 1 do maxHops
            {
                PingOptions options = new PingOptions(ttl, true);  // Nastavení TTL a povolení fragmentace
                PingReply reply = await ping.SendPingAsync(target, timeout, new byte[32], options);  // Asynchronní ping s nastavením

                Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms\t{reply.Status}");  // Výpis výsledku

                if (reply.Status == IPStatus.Success)  // Kontrola, zda jsme dosáhli cíle
                    break;  // Ukončení smyčky při dosažení cíle
            }
        }
    }
}

/*
Inicializace parametrů:
 target - Cílová IP adresa nebo doménové jméno
 maxHops - Maximální počet skoků k cíli
 timeout - Časová prodleva pro každý skok
Princip činnosti:
 Program postupně zvyšuje TTL (Time To Live) od 1
 Každý směrovač na cestě sníží TTL o 1 a při TTL=0 vrátí chybu
 Tím získáme sekvenci IP adres na trase k cíli
Výstupní formát:
 TTL číslo
 IP adresa směrovače
 Doba odezvy
 Stavový kód
Ukončení:
 Smyčka končí při dosažení cíle (IPStatus.Success)
 Nebo po dosažení maximálního počtu skoků
*/