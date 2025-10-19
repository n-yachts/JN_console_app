using System;  // Importování základních systémových knihoven
using System.Net;  // Importování knihoven pro práci se sítí včetně DNS

class DNSResolver  // Hlavní třída programu
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: DNSResolver <hostname>");
            return;  // Předčasné ukončení programu při chybném počtu argumentů
        }

        string hostname = args[0];  // Uložení prvního argumentu jako jméno hostitele

        try  // Ošetření možných chyb při DNS dotazu
        {
            // Získání všech IP adres pro zadané jméno hostitele
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

            // Výpis hlavičky s výsledky
            Console.WriteLine($"DNS záznamy pro {hostname}:");

            // Cyklus pro průchod všemi nalezenými IP adresami
            foreach (IPAddress address in addresses)
            {
                // Výpis IP adresy a jejího typu (IPv4/IPv6)
                Console.WriteLine($"  {address} ({address.AddressFamily})");
            }
        }
        catch (Exception ex)  // Zachycení jakékoliv výjimky během DNS dotazu
        {
            // Výpis chybové zprávy pro uživatele
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}

/*
Kontrola argumentů: Program nejprve zkontroluje, zda byl spuštěn s exactly jedním argumentem (jméno hostitele)
DNS dotaz: Pomocí metody Dns.GetHostAddresses() provede dotaz na DNS servery pro získání IP adres
Zpracování výsledků:
 Pro každou nalezenou IP adresu vypíše její textovou reprezentaci
 Zároveň uvádí typ adresy (IPv4 = InterNetwork, IPv6 = InterNetworkV6)
Ošetření chyb: Celý DNS dotaz je obalen v try-catch bloku pro zachycení možných chyb (neplatný hostname, chyba sítě, atd.)
*/