using System;           // Importování základních systémových knihoven pro práci s konzolí, výjimkami atd.
using System.Net;       // Importování knihoven pro síťové operace (DNS, IP adresy)
using System.Threading.Tasks;  // Importování knihoven pro asynchronní programování

class SimpleDNSServer    // Definice třídy pro náš jednoduchý DNS nástroj
{
    static async Task Main()  // Hlavní asynchronní metoda programu (async umožňuje použít await)
    {
        // Výpis informační zprávy do konzole
        Console.WriteLine("Jednoduchý DNS Server (demonstrační)");

        // Pole domén, které budeme testovat - můžete přidat vlastní
        string[] testDomains = { "google.com", "seznam.cz", "github.com" };

        // Procházení všech domén v poli pomocí foreach cyklu
        foreach (string domain in testDomains)
        {
            try  // Zachycení možných chyb při DNS dotazu
            {
                // Asynchronní získání všech IP adres pro danou doménu
                // await pozastaví provedení dokud není dotaz dokončen
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);

                // Výpis domény a nalezených IP adres
                Console.WriteLine($"\n{domain}:");

                // Procházení všech nalezených IP adres
                foreach (IPAddress addr in addresses)
                {
                    // Výpis IP adresy a jejího typu (IPv4/IPv6)
                    Console.WriteLine($"  {addr} ({addr.AddressFamily})");
                }
            }
            catch (Exception ex)  // Zachycení a zpracování výjimek
            {
                // Výpis chybové zprávy pokud se nepodaří přeložit doménu
                Console.WriteLine($"Chyba pro {domain}: {ex.Message}");
            }
        }
    }
}

/*
Knihovny a jmenné prostory:
 System - základní funkce jako práce s konzolí
 System.Net - síťové funkce včetně DNS
 System.Threading.Tasks - podpora asynchronního programování
Hlavní logika programu:
 Program vytvoří pole testovacích domén
 Pro každou doménu asynchronně provede DNS dotaz
 Zpracuje výsledky nebo chyby
 Vypíše nalezené IP adresy včetně jejich typu (IPv4/IPv6)
Důležité vlastnosti:
 Používá moderní asynchronní programování
 Ošetřuje výjimky pro jednotlivé domény
 Zobrazuje rodinu adres (IPv4 = InterNetwork, IPv6 = InterNetworkV6)
 Je to DNS klient, ne server - dotazuje se existující infrastruktury DNS
*/